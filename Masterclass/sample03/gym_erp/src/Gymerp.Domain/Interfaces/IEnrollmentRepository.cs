using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Domain.Interfaces
{
    public interface IEnrollmentRepository
    {
        Task<Enrollment> GetByIdAsync(Guid id);
        Task<Enrollment> GetByStudentIdAsync(Guid studentId);
        Task<IEnumerable<Enrollment>> GetAllAsync();
        Task AddAsync(Enrollment enrollment);
        Task UpdateAsync(Enrollment enrollment);
    }
} 