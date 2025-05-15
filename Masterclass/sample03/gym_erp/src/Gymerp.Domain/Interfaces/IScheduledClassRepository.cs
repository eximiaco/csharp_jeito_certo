using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Domain.Interfaces
{
    public interface IScheduledClassRepository
    {
        Task<IEnumerable<ScheduledClass>> GetByEnrollmentAndDateAsync(Guid enrollmentId, DateTime date);
        Task<IEnumerable<ScheduledClass>> GetByDateAsync(DateTime date);
    }
} 