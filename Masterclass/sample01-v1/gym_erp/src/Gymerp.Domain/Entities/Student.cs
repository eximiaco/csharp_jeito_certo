using System;
using System.Collections.Generic;

namespace Gymerp.Domain.Entities
{
    public class Student
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Phone { get; private set; } = string.Empty;
        public string Document { get; private set; } = string.Empty;
        public DateTime BirthDate { get; private set; }
        public Gender Gender { get; private set; }
        public string Address { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public virtual ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();
        public virtual ICollection<PhysicalAssessment> PhysicalAssessments { get; private set; } = new List<PhysicalAssessment>();

        protected Student() { }

        public Student(string name, string email, string phone, string document, DateTime birthDate, Gender gender, string address)
        {
            Id = Guid.NewGuid();
            Name = name;
            Email = email;
            Phone = phone;
            Document = document;
            BirthDate = birthDate;
            Gender = gender;
            Address = address;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Enrollments = new List<Enrollment>();
            PhysicalAssessments = new List<PhysicalAssessment>();
        }

        public void Update(string name, string email, string phone, string address)
        {
            Name = name;
            Email = email;
            Phone = phone;
            Address = address;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public enum Gender
    {
        Male,
        Female,
        Other
    }
} 