using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Infrastructure.Repositories
{
    public class AccessRecordRepository : IAccessRecordRepository
    {
        private readonly ApplicationDbContext _context;

        public AccessRecordRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AccessRecord?> GetByIdAsync(Guid id)
        {
            var record = await _context.AccessRecords
                .Include(r => r.Enrollment)
                .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null)
            {
                throw new InvalidOperationException($"Registro de acesso com ID {id} não encontrado.");
            }

            return record;
        }

        public async Task<IEnumerable<AccessRecord>> GetByEnrollmentIdAsync(Guid enrollmentId)
        {
            return await _context.AccessRecords
                .Where(ar => ar.EnrollmentId == enrollmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<AccessRecord>> GetByDateAsync(DateTime date)
        {
            return await _context.AccessRecords
                .Include(r => r.Enrollment)
                .ThenInclude(e => e.Student)
                .Where(r => r.AccessTime.Date == date.Date)
                .ToListAsync();
        }

        public async Task<AccessRecord> GetByEnrollmentAndDateTimeAsync(Guid enrollmentId, DateTime dateTime)
        {
            return await _context.AccessRecords
                .FirstOrDefaultAsync(ar => 
                    ar.EnrollmentId == enrollmentId && 
                    ar.AccessTime.Date == dateTime.Date &&
                    ar.AccessTime.Hour == dateTime.Hour &&
                    ar.AccessTime.Minute == dateTime.Minute);
        }

        public async Task AddAsync(AccessRecord record)
        {
            await _context.AccessRecords.AddAsync(record);
            await _context.SaveChangesAsync();
        }
    }
} 