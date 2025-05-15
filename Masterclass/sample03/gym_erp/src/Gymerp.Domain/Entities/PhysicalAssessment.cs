using System;

namespace Gymerp.Domain.Entities
{
    public class PhysicalAssessment
    {
        public Guid Id { get; private set; }
        public Guid StudentId { get; private set; }
        public Guid PersonalId { get; private set; }
        public DateTime AssessmentDate { get; private set; }
        public decimal Weight { get; private set; }
        public decimal Height { get; private set; }
        public decimal BodyFatPercentage { get; private set; }
        public string Notes { get; private set; } = string.Empty;
        public virtual Student? Student { get; private set; }
        public virtual Personal? Personal { get; private set; }
        public PhysicalAssessmentStatus Status { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime ScheduledDate { get; private set; }

        protected PhysicalAssessment() { }

        public PhysicalAssessment(
            Guid studentId,
            Guid personalId,
            DateTime assessmentDate,
            decimal weight,
            decimal height,
            decimal bodyFatPercentage,
            string notes)
        {
            Id = Guid.NewGuid();
            StudentId = studentId;
            PersonalId = personalId;
            AssessmentDate = assessmentDate;
            Weight = weight;
            Height = height;
            BodyFatPercentage = bodyFatPercentage;
            Notes = notes;
            Status = PhysicalAssessmentStatus.Scheduled;
            ScheduledDate = assessmentDate;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Complete()
        {
            Status = PhysicalAssessmentStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            Status = PhysicalAssessmentStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reschedule(DateTime newDate)
        {
            ScheduledDate = newDate;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public enum PhysicalAssessmentStatus
    {
        Scheduled,
        Completed,
        Cancelled
    }
} 