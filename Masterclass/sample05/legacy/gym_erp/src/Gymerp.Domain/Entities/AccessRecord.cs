using System;

namespace Gymerp.Domain.Entities
{
    public class AccessRecord
    {
        public Guid Id { get; private set; }
        public Guid EnrollmentId { get; private set; }
        public DateTime AccessTime { get; private set; }
        public virtual Enrollment Enrollment { get; private set; }

        protected AccessRecord() { }

        public AccessRecord(Guid enrollmentId, DateTime accessTime)
        {
            Id = Guid.NewGuid();
            EnrollmentId = enrollmentId;
            AccessTime = accessTime;
        }
    }
} 