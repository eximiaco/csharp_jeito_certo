using System;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;
using Gymerp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Gymerp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentController(IFullEnrollmentService fullEnrollmentService) : ControllerBase
    {
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
    }
} 