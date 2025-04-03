using WorkflowCore.Interface;
using OrderService.Workflow.Steps;

namespace OrderService.Workflow;

public class OrderWorflowData
{
    public string OrderId { get; set; } = string.Empty;
}

public class OrderWorkflowDefinition(ILogger<OrderWorkflowDefinition> logger) : IWorkflow<OrderWorflowData>
{
    public string Id => "OrderProcessingWorkflow";

    public int Version => 1;

    public void Build(IWorkflowBuilder<OrderWorflowData> builder)
    {
        builder
            .StartWith(_ => logger.LogInformation("Starting order processing workflow"))
            .Saga(saga => saga
                .StartWith<ReserveStockStep>()
                    .Input(step => step.OrderId, data => data.OrderId)
                .Then<ProcessPaymentStep>()
                    .Input(step => step.OrderId, data => data.OrderId)
                .Then<ScheduleShippingStep>()
                    .Input(step => step.OrderId, data => data.OrderId)
                .Then<FinishOrderStep>()
                    .Input(step => step.OrderId, data => data.OrderId))
            .CompensateWith<UndoProcessingStep>(compensate =>
                compensate.Input(step => step.OrderId, data => data.OrderId));
    }
}