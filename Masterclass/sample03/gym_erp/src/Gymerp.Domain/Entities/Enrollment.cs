using System;
using System.Collections.Generic;

namespace Gymerp.Domain.Entities
{
    public class Enrollment
    {
        public Guid Id { get; private set; }
        public Guid StudentId { get; private set; }
        public Guid PlanId { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public EnrollmentStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public virtual Student? Student { get; private set; }
        public virtual Plan? Plan { get; private set; }
        public virtual ICollection<Payment> Payments { get; private set; } = new List<Payment>();

        protected Enrollment() { }

        public Enrollment(Guid studentId, Guid planId, DateTime startDate, DateTime endDate)
        {
            Id = Guid.NewGuid();
            StudentId = studentId;
            PlanId = planId;
            StartDate = startDate;
            EndDate = endDate;
            Status = EnrollmentStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Confirm()
        {
            Status = EnrollmentStatus.Active;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            Status = EnrollmentStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Complete()
        {
            Status = EnrollmentStatus.Completed;
        }
    }

    public enum EnrollmentStatus
    {
        Pending,
        Active,
        Cancelled,
        Completed
    }
} 