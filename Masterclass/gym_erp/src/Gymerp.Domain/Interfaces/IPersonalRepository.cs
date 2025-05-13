using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Domain.Interfaces
{
    public interface IPersonalRepository
    {
        Task<Personal> GetByIdAsync(Guid id);
        Task<Personal> GetByDocumentAsync(string document);
        Task<Personal> AddAsync(Personal personal);
        Task<Personal> UpdateAsync(Personal personal);
    }
} 