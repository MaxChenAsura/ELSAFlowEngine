using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace FlowEngineDemo.Workflows;

public class PurchaseApprovalWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId("PURCHASE_APPROVAL");
        builder.Name = "PURCHASE_APPROVAL";

        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("PURCHASE_APPROVAL 流程開始"),
                new WriteLine("採購單流程測試"),
                new WriteLine("PURCHASE_APPROVAL 流程結束")
            }
        };
    }
}