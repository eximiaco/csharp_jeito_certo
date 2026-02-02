using FinanceManager.BankAccounts;
using FinanceManager.Crosscutting.Messaging;

namespace FinanceManager.Crosscutting
{
    public class EventDispatcherService(QueueService queueService)
    {
        public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> events)
        {
            foreach(var ev in events)
            {
                if(ev.GetType() == typeof(BudgetAlertDomainEvent))
                {
                    await queueService.PublishAsync("FinanceManager.BudgetAlerts.BudgetExceeded", ev);
                }
            }
        }
    }
}
