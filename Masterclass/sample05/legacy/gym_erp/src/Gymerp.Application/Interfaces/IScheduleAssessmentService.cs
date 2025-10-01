using System;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;

namespace Gymerp.Application.Interfaces
{
    public interface IScheduleAssessmentService
    {
        Task<Guid> ScheduleAssessmentAsync(ScheduleAssessmentDto dto);
    }
}
