using FlowEngineDemo.Services;
using FlowEngineDemo.Data;
using Microsoft.EntityFrameworkCore;
namespace FlowEngineDemo.Extensions;

public static class IServiceCollectionExtension
{
    public static IServiceCollection AddFlowEngineDatabase(
     this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("Elsa")
                ?? throw new InvalidOperationException(
                    "Connection string 'Elsa' not found.");

            services.AddDbContext<FlowEngineDbContext>(
                options =>
                    options.UseSqlServer(connectionString));

            return services;
        }
    public static IServiceCollection AddCustomizations(
        this IServiceCollection services)
    {
        return services

            // Service
            .AddScoped<
                IApprovalWorkflowService,
                ApprovalWorkflowService>()

            .AddScoped<
                IApprovalHistoryService,
                ApprovalHistoryService>();

            // DAO
            // 後續建立 DAO 後統一放在這裡
    }
}