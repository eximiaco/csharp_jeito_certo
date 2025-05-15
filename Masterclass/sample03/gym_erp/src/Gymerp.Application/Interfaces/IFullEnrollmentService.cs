using System;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;

namespace Gymerp.Application.Interfaces
{
    public interface IFullEnrollmentService
    {
        Task<Guid> EnrollAsync(FullEnrollmentDto dto);
    }
} 