using KinTraffic.Api.Persistence;
using Microsoft.AspNetCore.Identity;

namespace KinTraffic.Api.Services.Security;

public static class IdentityPasswordPolicy
{
    public static IdentityBuilder AddKinTrafficPasswordPolicy(
        this IdentityBuilder builder,
        AuthenticationOptions options)
    {
        builder.Services.Configure<IdentityOptions>(identity =>
        {
            identity.Password.RequiredLength = options.PasswordMinimumLength ?? 0;
            identity.Password.RequiredUniqueChars = 1;
            identity.Password.RequireDigit = false;
            identity.Password.RequireLowercase = false;
            identity.Password.RequireUppercase = false;
            identity.Password.RequireNonAlphanumeric = false;
        });
        builder.Services.Configure<PasswordHasherOptions>(hasher =>
        {
            hasher.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
            hasher.IterationCount = options.PasswordHasherIterationCount ?? 0;
        });
        return builder.AddPasswordValidator<MaximumPasswordLengthValidator>();
    }
}

public sealed class MaximumPasswordLengthValidator : IPasswordValidator<KinTrafficIdentityAccount>
{
    private readonly AuthenticationOptions _options;

    public MaximumPasswordLengthValidator(AuthenticationOptions options)
    {
        _options = options;
    }

    public Task<IdentityResult> ValidateAsync(
        UserManager<KinTrafficIdentityAccount> manager,
        KinTrafficIdentityAccount user,
        string? password) =>
        Task.FromResult(password is not null && password.Length > _options.PasswordMaximumLength
            ? IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooLong",
                Description = $"Password must be at most {_options.PasswordMaximumLength} characters."
            })
            : IdentityResult.Success);
}