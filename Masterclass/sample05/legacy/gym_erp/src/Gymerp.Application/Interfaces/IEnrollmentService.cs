using System;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;

namespace Gymerp.Application.Interfaces
{
    public interface IEnrollmentService
    {
        Task<Guid> CreateEnrollmentAsync(EnrollmentDto dto);
    }
}
