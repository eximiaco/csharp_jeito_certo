using System;

namespace Gymerp.Application.DTOs
{
    public class PhysicalAssessmentDto
    {
        public Guid StudentId { get; set; }
        public Guid PersonalId { get; set; }
        public DateTime AssessmentDate { get; set; }
        public decimal Weight { get; set; }
        public decimal Height { get; set; }
        public decimal BodyFatPercentage { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
} 