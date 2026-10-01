using Elsa.Workflows;
using Elsa.Workflows.Activities;
using FlowEngineDemo.Activities;
using Elsa.Workflows.Memory;
namespace FlowEngineDemo.Workflows;

public class GeneralApprovalWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId("GENERAL_APPROVAL");

        builder.Name = "GENERAL_APPROVAL";
        // 放在這裡
        var approvalDecision =
            builder.WithVariable<string>(
                "ApprovalDecision",
                string.Empty);

        var waitForApproval = new WaitForApproval
        {
            Decision = approvalDecision
        };
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("GENERAL_APPROVAL 流程開始"),
                new WriteLine("申請單已送出"),
                // 真正停在這裡等待外部簽核
                waitForApproval,
               new If(context =>
                    approvalDecision.Get(context)?.ToString() == "APPROVE")
                {
                    Then = new WriteLine("主管簽核完成"),
                    Else = new WriteLine("主管退回申請")
                },
            new WriteLine("GENERAL_APPROVAL 流程結束")
            }
        };
    }
}