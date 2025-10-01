using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Domain.Interfaces
{
    public interface IAccessRecordRepository
    {
        Task<IEnumerable<AccessRecord>> GetByEnrollmentIdAsync(Guid enrollmentId);
        Task<IEnumerable<AccessRecord>> GetByDateAsync(DateTime date);
        Task<AccessRecord> GetByEnrollmentAndDateTimeAsync(Guid enrollmentId, DateTime dateTime);
    }
} 