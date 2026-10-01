using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using FlowEngineDemo.Activities;
using FlowEngineDemo.Workflows;

namespace FlowEngineDemo.Extensions;

public static class ElsaExtension
{
    public static IServiceCollection AddElsaFlowEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Elsa")
            ?? throw new InvalidOperationException(
                "Connection string 'Elsa' not found.");

        services.AddElsa(elsa =>
        {
            // Elsa Workflow Management
            elsa.UseWorkflowManagement(management =>
            {
                management.UseEntityFrameworkCore(ef =>
                {
                    ef.UseSqlServer(connectionString);
                    ef.RunMigrations = true;
                });
            });

            // Elsa Workflow Runtime
            elsa.UseWorkflowRuntime(runtime =>
            {
                runtime.UseEntityFrameworkCore(ef =>
                {
                    ef.UseSqlServer(connectionString);
                    ef.RunMigrations = true;
                });
            });

            // Custom Activities
            elsa.AddActivity<WaitForApproval>();

            // Workflow Templates
            elsa.AddWorkflowsFrom<GeneralApprovalWorkflow>();
        });

        return services;
    }
}