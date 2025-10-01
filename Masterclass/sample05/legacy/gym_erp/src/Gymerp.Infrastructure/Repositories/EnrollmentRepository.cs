using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Infrastructure.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Enrollment> GetByIdAsync(Guid id)
        {
            return (await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Plan)
                .FirstOrDefaultAsync(e => e.Id == id))!;
        }

        public async Task<Enrollment> GetByStudentIdAsync(Guid studentId)
        {
            return (await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Plan)
                .FirstOrDefaultAsync(e => e.StudentId == studentId))!;
        }

        public async Task<IEnumerable<Enrollment>> GetAllAsync()
        {
            return await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Plan)
                .ToListAsync();
        }

        public async Task AddAsync(Enrollment enrollment)
        {
            await _context.Enrollments.AddAsync(enrollment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Enrollment enrollment)
        {
            _context.Enrollments.Update(enrollment);
            await _context.SaveChangesAsync();
        }
    }
} 