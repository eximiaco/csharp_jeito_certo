using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Infrastructure.Repositories
{
    public class PhysicalAssessmentRepository : IPhysicalAssessmentRepository
    {
        private readonly ApplicationDbContext _context;

        public PhysicalAssessmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PhysicalAssessment> GetByIdAsync(Guid id)
        {
            return await _context.PhysicalAssessments
                .Include(a => a.Student)
                .Include(a => a.Personal)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<PhysicalAssessment>> GetByStudentIdAsync(Guid studentId)
        {
            return await _context.PhysicalAssessments
                .Include(a => a.Student)
                .Include(a => a.Personal)
                .Where(a => a.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhysicalAssessment>> GetByDateAsync(DateTime date)
        {
            return await _context.PhysicalAssessments
                .Include(a => a.Student)
                .Include(a => a.Personal)
                .Where(a => a.ScheduledDate.Date == date.Date)
                .ToListAsync();
        }

        public async Task<PhysicalAssessment> AddAsync(PhysicalAssessment assessment)
        {
            await _context.PhysicalAssessments.AddAsync(assessment);
            await _context.SaveChangesAsync();
            return assessment;
        }

        public async Task<PhysicalAssessment> UpdateAsync(PhysicalAssessment assessment)
        {
            _context.PhysicalAssessments.Update(assessment);
            await _context.SaveChangesAsync();
            return assessment;
        }
    }
} 