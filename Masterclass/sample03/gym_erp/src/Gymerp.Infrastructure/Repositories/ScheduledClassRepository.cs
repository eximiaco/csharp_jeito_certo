using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Infrastructure.Repositories
{
    public class ScheduledClassRepository : IScheduledClassRepository
    {
        private readonly ApplicationDbContext _context;

        public ScheduledClassRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IEnumerable<ScheduledClass>> GetByEnrollmentAndDateAsync(Guid enrollmentId, DateTime date)
        {
            return await _context.ScheduledClasses
                .Where(sc => sc.EnrollmentId == enrollmentId && sc.ScheduledTime.Date == date.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<ScheduledClass>> GetByDateAsync(DateTime date)
        {
            return await _context.ScheduledClasses
                .Where(sc => sc.ScheduledTime.Date == date.Date)
                .Include(sc => sc.Enrollment)
                    .ThenInclude(e => e.Student)
                .ToListAsync();
        }
    }
} 