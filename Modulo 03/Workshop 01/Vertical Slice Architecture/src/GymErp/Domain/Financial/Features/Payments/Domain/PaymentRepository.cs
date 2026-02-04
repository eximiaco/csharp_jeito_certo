using GymErp.Domain.Financial.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GymErp.Domain.Financial.Features.Payments.Domain;

public class PaymentRepository(FinancialDbContext context)
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        await context.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Payments
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
