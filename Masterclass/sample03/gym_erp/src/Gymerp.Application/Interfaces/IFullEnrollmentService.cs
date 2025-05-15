using System;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;
using Gymerp.Domain.Entities;

namespace Gymerp.Application.Interfaces
{
    public interface IFullEnrollmentService
    {
        Task<Enrollment> EnrollAsync(FullEnrollmentDto dto);
    }
} 