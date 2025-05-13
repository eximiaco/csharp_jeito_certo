using System;
using Gymerp.Domain.Entities;

namespace Gymerp.Application.DTOs
{
    public class EnrollmentDto
    {
        public StudentDto Student { get; set; }
        public Guid PlanId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
} 