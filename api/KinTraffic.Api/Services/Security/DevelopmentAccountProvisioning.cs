using System.Net;
using System.Text;
using KinTraffic.Api.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KinTraffic.Api.Services.Security;

public static class DevelopmentAccountProvisioning
{
    public const string CommandFlag = "--provision-account";
    public const string UserNameFlag = "--username";

    public static bool IsRequested(string[] args) => args.Contains(CommandFlag, StringComparer.Ordinal);

    public static void EnsureAllowed(IHostEnvironment environment, string[] args)
    {
        if (!IsRequested(args))
        {
            throw new InvalidOperationException($"The explicit '{CommandFlag}' flag is required.");
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Account provisioning is available only in the Development environment.");
        }
    }

    public static async Task<int> RunAsync(
        IConfiguration configuration,
        IHostEnvironment environment,
        string[] args)
    {
        EnsureAllowed(environment, args);
        var userName = GetArgumentValue(args, UserNameFlag);
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new InvalidOperationException($"Provide an account name with '{UserNameFlag}'.");
        }

        var connectionString = configuration.GetConnectionString("KinTraffic");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:KinTraffic must be configured for provisioning.");
        }

        var authentication = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>();
        if (authentication?.MaxFailedAccessAttempts is null or <= 0 ||
            authentication.LockoutDurationMinutes is null or <= 0)
        {
            throw new InvalidOperationException("Identity lockout settings must be configured before provisioning.");
        }

        if (Console.IsInputRedirected)
        {
            throw new InvalidOperationException("Provisioning requires an interactive secure password prompt.");
        }

        Console.Write("New account password: ");
        var password = ReadPassword();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<KinTrafficDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentityCore<KinTrafficIdentityAccount>(options =>
        {
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = authentication.MaxFailedAccessAttempts.Value;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(authentication.LockoutDurationMinutes.Value);
        })
            .AddEntityFrameworkStores<KinTrafficDbContext>();

        await using var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var account = new KinTrafficIdentityAccount { UserName = userName, Email = userName };
        var result = await userManager.CreateAsync(account, password);
        if (!result.Succeeded)
        {
            Console.Error.WriteLine("Account provisioning failed validation.");
            return 1;
        }

        Console.WriteLine("Development account provisioned.");
        return 0;
    }

    private static string? GetArgumentValue(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string ReadPassword()
    {
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return password.ToString();
            }

            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }
    }
}