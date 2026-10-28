using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart;
using Elsa.Workflows.Models;
using Elsa.Workflows.Memory;
using FlowEngineDemo.Activities;
namespace FlowEngineDemo.Workflows;

public class PurchaseApprovalWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId("PURCHASE_APPROVAL");
        builder.Name = "PURCHASE_APPROVAL";

         var approvalDecision =
            builder.WithVariable<string>(
                "ApprovalDecision",
                string.Empty);

        var waitForApproval = new WaitForApproval
        {
            Decision = approvalDecision
            //ApproverId = "M001"
        };

        var approvalCompleted =
            builder.WithVariable<bool>(
                "ApprovalCompleted",
                false);
        builder.Root = new Sequence
        {
            Variables =
            {
                approvalDecision,
                approvalCompleted
            },
              Activities =
                {
                    new WriteLine("採購簽核流程開始"),

                    new While(context =>
                        !approvalCompleted.Get(context))
                    {
                        Body = new Sequence
                        {
                            Activities =
                            {
                                new WaitForApproval
                                {
                                      Decision = approvalDecision
                                },

                                new If(context =>
                                    approvalDecision.Get(context) == "APPROVE")
                                {
                                    Then = new Sequence
                                    {
                                        Activities =
                                        {
                                            new WriteLine("主管簽核：APPROVE"),

                                            new SetVariable<bool>
                                            {
                                                 Variable = approvalCompleted,
                                                Value = new(context => true)
                                            }
                                        }
                                    },

                                    Else = new Sequence
                                    {
                                        Activities =
                                        {
                                            new WriteLine("主管簽核：REJECT"),

                                            new WriteLine("等待申請人修改"),

                                            new WaitForRevision(),

                                            new WriteLine("申請人重新送出，重新進入主管簽核")
                                        }
                                    }
                                }
                            }
                        }
                    },

                    new WriteLine("採購簽核流程結束")
                }
        };
    }
}