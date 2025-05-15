using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment> GetByIdAsync(Guid id);
        Task<IEnumerable<Payment>> GetByEnrollmentIdAsync(Guid enrollmentId);
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<Payment> AddAsync(Payment payment);
        Task<Payment> UpdateAsync(Payment payment);
    }
} 