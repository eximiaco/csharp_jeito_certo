using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> GetByIdAsync(Guid id)
        {
            return (await _context.Payments
                .Include(p => p.Enrollment)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(p => p.Id == id))!;
        }

        public async Task<IEnumerable<Payment>> GetByEnrollmentIdAsync(Guid enrollmentId)
        {
            return await _context.Payments
                .Include(p => p.Enrollment)
                    .ThenInclude(e => e.Student)
                .Where(p => p.EnrollmentId == enrollmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            return await _context.Payments
                .Include(p => p.Enrollment)
                    .ThenInclude(e => e.Student)
                .ToListAsync();
        }

        public async Task<Payment> AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }
    }
} 