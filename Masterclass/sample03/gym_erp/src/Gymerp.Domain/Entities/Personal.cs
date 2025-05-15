using System;
using System.Collections.Generic;

namespace Gymerp.Domain.Entities
{
    public class Personal
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Phone { get; private set; } = string.Empty;
        public string Document { get; private set; } = string.Empty;
        public string Specialization { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public virtual ICollection<PhysicalAssessment> PhysicalAssessments { get; private set; } = new List<PhysicalAssessment>();

        protected Personal() { }

        public Personal(string name, string email, string phone, string document, string specialization)
        {
            Id = Guid.NewGuid();
            Name = name;
            Email = email;
            Phone = phone;
            Document = document;
            Specialization = specialization;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            PhysicalAssessments = new List<PhysicalAssessment>();
        }

        public void Update(string name, string email, string phone, string specialization)
        {
            Name = name;
            Email = email;
            Phone = phone;
            Specialization = specialization;
            UpdatedAt = DateTime.UtcNow;
        }
    }
} 