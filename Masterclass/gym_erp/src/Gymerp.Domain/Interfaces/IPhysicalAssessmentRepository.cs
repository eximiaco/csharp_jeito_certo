using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Domain.Interfaces
{
    public interface IPhysicalAssessmentRepository
    {
        Task<PhysicalAssessment> GetByIdAsync(Guid id);
        Task<IEnumerable<PhysicalAssessment>> GetByStudentIdAsync(Guid studentId);
        Task<IEnumerable<PhysicalAssessment>> GetByDateAsync(DateTime date);
        Task<PhysicalAssessment> AddAsync(PhysicalAssessment assessment);
        Task<PhysicalAssessment> UpdateAsync(PhysicalAssessment assessment);
    }
} 