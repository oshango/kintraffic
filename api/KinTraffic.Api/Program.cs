using KinTraffic.Api.Repositories.Assets;
using KinTraffic.Api.Repositories.Audit;
using KinTraffic.Api.Repositories.Billing;
using KinTraffic.Api.Repositories.Contracts;
using KinTraffic.Api.Repositories.Faults;
using KinTraffic.Api.Repositories.Inventory;
using KinTraffic.Api.Repositories.Traffic;
using KinTraffic.Api.Repositories.WorkOrders;
using KinTraffic.Api.Services.Assets;
using KinTraffic.Api.Services.Audit;
using KinTraffic.Api.Services.Billing;
using KinTraffic.Api.Services.Contracts;
using KinTraffic.Api.Services.Faults;
using KinTraffic.Api.Services.Inventory;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Services.Traffic;
using KinTraffic.Api.Services.WorkOrders;
using KinTraffic.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

if (DevelopmentAccountProvisioning.IsRequested(args))
{
	Environment.ExitCode = await DevelopmentAccountProvisioning.RunAsync(builder.Configuration, builder.Environment, args);
	return;
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IValidateOptions<AuthenticationOptions>, AuthenticationOptionsValidator>();
builder.Services.AddOptions<AuthenticationOptions>()
	.Bind(builder.Configuration.GetSection(AuthenticationOptions.SectionName))
	.ValidateOnStart();
var configuredAuthentication = builder.Configuration
	.GetSection(AuthenticationOptions.SectionName)
	.Get<AuthenticationOptions>() ?? new AuthenticationOptions();
builder.Services.AddSingleton(configuredAuthentication);

if (configuredAuthentication.AllowedOrigins is { Length: > 0 } origins)
{
	builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
		policy.WithOrigins(origins)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.WithExposedHeaders("Authorization")
			.AllowCredentials()));
}

var databaseConnectionString = builder.Configuration.GetConnectionString("KinTraffic");
if (string.IsNullOrWhiteSpace(databaseConnectionString))
{
	throw new InvalidOperationException("ConnectionStrings:KinTraffic must be configured for SQL Server persistence.");
}

builder.Services.AddDbContext<KinTrafficDbContext>(options =>
	options.UseSqlServer(databaseConnectionString));
var useTrustedForwardedHeaders = builder.Services.AddAuthenticationRateLimiting(configuredAuthentication);
builder.Services.AddIdentityCore<KinTrafficIdentityAccount>(options =>
{
	options.Lockout.AllowedForNewUsers = true;
	options.Lockout.MaxFailedAccessAttempts = configuredAuthentication.MaxFailedAccessAttempts ?? 0;
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(configuredAuthentication.LockoutDurationMinutes ?? 0);
})
	.AddSignInManager()
	.AddEntityFrameworkStores<KinTrafficDbContext>();
builder.Services.AddScoped<CredentialVerificationService>();
builder.Services.AddSingleton<SigningKeyRing>();
builder.Services.AddSingleton<AccessTokenService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddSingleton<AccountAttemptLimiter>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
	.Configure<AccessTokenService>((options, tokenService) =>
	{
		options.MapInboundClaims = false;
		options.IncludeErrorDetails = false;
		options.TokenValidationParameters = tokenService.CreateValidationParameters();
	});
builder.Services.AddScoped<ITrafficRepository, SqlTrafficRepository>();
builder.Services.AddScoped<TrafficService>();
builder.Services.AddScoped<RoadSegmentRepository>();
builder.Services.AddScoped<RoadSegmentService>();
builder.Services.AddScoped<TrafficStateRepository>();
builder.Services.AddScoped<TrafficStateService>();

builder.Services.AddScoped<AssetRepository>();
builder.Services.AddScoped<AssetService>();

builder.Services.AddScoped<FaultRepository>();
builder.Services.AddScoped<FaultService>();

builder.Services.AddScoped<WorkOrderRepository>();
builder.Services.AddScoped<WorkOrderService>();

builder.Services.AddScoped<InventoryRepository>();
builder.Services.AddScoped<InventoryService>();

builder.Services.AddScoped<ContractRepository>();
builder.Services.AddScoped<ContractService>();

builder.Services.AddScoped<BillingRepository>();
builder.Services.AddScoped<BillingService>();

builder.Services.AddScoped<AuditRepository>();
builder.Services.AddScoped<AuditService>();

var app = builder.Build();
_ = app.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

if (useTrustedForwardedHeaders)
{
	app.UseForwardedHeaders();
}

await using (var scope = app.Services.CreateAsyncScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<KinTrafficDbContext>();
	await dbContext.Database.MigrateAsync();
}

app.MapOpenApi();
app.UseRouting();
if (configuredAuthentication.AllowedOrigins is { Length: > 0 })
{
	app.UseCors("Frontend");
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

