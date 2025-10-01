using System;

namespace Gymerp.Domain.Entities
{
    public class ScheduledClass
    {
        public Guid Id { get; private set; }
        public Guid EnrollmentId { get; private set; }
        public DateTime ScheduledTime { get; private set; }
        public string ClassName { get; private set; } = string.Empty;
        public virtual Enrollment? Enrollment { get; private set; }

        protected ScheduledClass() { }

        public ScheduledClass(Guid enrollmentId, DateTime scheduledTime, string className)
        {
            Id = Guid.NewGuid();
            EnrollmentId = enrollmentId;
            ScheduledTime = scheduledTime;
            ClassName = className;
        }
    }
} 