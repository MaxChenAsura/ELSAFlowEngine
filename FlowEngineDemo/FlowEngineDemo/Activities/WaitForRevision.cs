using Elsa.Workflows;

namespace FlowEngineDemo.Activities;

public class WaitForRevision : Activity
{
    protected override void Execute(
        ActivityExecutionContext context)
    {
        var applicantId =
            context.WorkflowInput.TryGetValue(
                "ApplicantId",
                out var applicantValue)
                ? applicantValue?.ToString()
                : string.Empty;

        Console.WriteLine(
            $"等待申請人修改：{applicantId}");

        context.CreateBookmark(
            "REVISION",
            OnResume);
    }

    private async ValueTask OnResume(
        ActivityExecutionContext context)
    {
        Console.WriteLine("申請人已重新送出");

        await context.CompleteActivityAsync();
    }
}