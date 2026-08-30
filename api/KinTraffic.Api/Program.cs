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
using KinTraffic.Api.Services.Traffic;
using KinTraffic.Api.Services.WorkOrders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddScoped<TrafficRepository>();
builder.Services.AddScoped<TrafficService>();

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

app.MapOpenApi();
app.MapControllers();

app.Run();

