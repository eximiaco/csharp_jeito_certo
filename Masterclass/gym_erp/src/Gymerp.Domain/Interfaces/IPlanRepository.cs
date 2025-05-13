using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Domain.Interfaces
{
    public interface IPlanRepository
    {
        Task<Plan> GetByIdAsync(Guid id);
        Task<IEnumerable<Plan>> GetAllAsync();
        Task<Plan> AddAsync(Plan plan);
        Task<Plan> UpdateAsync(Plan plan);
    }
} 