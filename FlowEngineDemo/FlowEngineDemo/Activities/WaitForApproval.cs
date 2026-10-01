using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Memory;

namespace FlowEngineDemo.Activities;

public class WaitForApproval : Activity
{
    public Variable<string> Decision { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
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