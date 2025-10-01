using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Infrastructure.Repositories
{
    public class PlanRepository : IPlanRepository
    {
        private readonly ApplicationDbContext _context;

        public PlanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Plan?> GetByIdAsync(Guid id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan == null)
            {
                throw new InvalidOperationException($"Plano com ID {id} não encontrado.");
            }
            return plan;
        }

        public async Task<IEnumerable<Plan>> GetAllAsync()
        {
            return await _context.Plans.ToListAsync();
        }

        public async Task<Plan> AddAsync(Plan plan)
        {
            await _context.Plans.AddAsync(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task<Plan> UpdateAsync(Plan plan)
        {
            _context.Plans.Update(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task DeleteAsync(Guid id)
        {
            var plan = await GetByIdAsync(id);
            _context.Plans.Remove(plan);
            await _context.SaveChangesAsync();
        }
    }
} 