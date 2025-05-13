using System;
using Gymerp.Domain.Entities;

namespace Gymerp.Application.DTOs
{
    public class FullEnrollmentDto
    {
        public StudentDto Student { get; set; }
        public Guid PlanId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public PhysicalAssessmentDto PhysicalAssessment { get; set; } // Agendamento obrigatório
    }
} 