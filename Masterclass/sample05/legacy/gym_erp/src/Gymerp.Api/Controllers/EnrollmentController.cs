using System;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;
using Gymerp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Gymerp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentController(
        IFullEnrollmentService fullEnrollmentService,
        IEnrollmentService enrollmentService,
        IScheduleAssessmentService scheduleAssessmentService,
        IProcessPaymentService processPaymentService) : ControllerBase
    {
        [HttpPost("create")]
        public async Task<IActionResult> CreateEnrollment([FromBody] EnrollmentDto dto)
        {
            try
            {
                var enrollmentId = await enrollmentService.CreateEnrollmentAsync(dto);
                return Ok(new { EnrollmentId = enrollmentId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("enroll")]
        public async Task<IActionResult> Enroll([FromBody] FullEnrollmentDto dto)
        {
            try
            {
                var enrollmentId = await fullEnrollmentService.EnrollAsync(dto);
                return Ok(new { EnrollmentId = enrollmentId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("schedule-assessment")]
        public async Task<IActionResult> ScheduleAssessment([FromBody] ScheduleAssessmentDto dto)
        {
            try
            {
                var assessmentId = await scheduleAssessmentService.ScheduleAssessmentAsync(dto);
                return Ok(new { AssessmentId = assessmentId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("process-payment")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
        {
            try
            {
                var paymentResult = await processPaymentService.ProcessPaymentAsync(dto);
                return Ok(paymentResult);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
} 