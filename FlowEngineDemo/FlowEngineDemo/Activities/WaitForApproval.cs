using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Memory;

namespace FlowEngineDemo.Activities;

public class WaitForApproval : Activity
{
    public Variable<string> Decision { get; set; } = default!;

    // 此關簽核人員
    
    protected override void Execute(ActivityExecutionContext context)
    {
         var approvalMode =
            context.WorkflowInput.TryGetValue(
                "ApprovalMode",
                out var approvalModeValue)
                ? approvalModeValue?.ToString()
                : "SINGLE";

        var approverIds =
            context.WorkflowInput.TryGetValue(
                "ApproverIds",
                out var approverIdsValue)
                ? approverIdsValue?.ToString()
                : string.Empty;

        Console.WriteLine(
            $"簽核模式：{approvalMode}");

        Console.WriteLine(
            $"等待主管簽核：{approverIds}");

        context.CreateBookmark(
            "APPROVAL",
            OnResume);
    }

    private async ValueTask OnResume(
        ActivityExecutionContext context)
    {
        var decision =
            context.WorkflowInput.TryGetValue(
                "Decision",
                out var decisionValue)
                ? decisionValue?.ToString()
                : null;

        Console.WriteLine($"簽核結果：{decision}");

        context.Set(
            Decision,
            decision ?? string.Empty);

        await context.CompleteActivityAsync();
    }
}