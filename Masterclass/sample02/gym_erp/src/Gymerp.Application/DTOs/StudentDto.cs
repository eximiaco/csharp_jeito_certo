using System;
using Gymerp.Domain.Entities;

namespace Gymerp.Application.DTOs
{
    public class StudentDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Document { get; set; }
        public DateTime BirthDate { get; set; }
        public Gender Gender { get; set; }
        public string Address { get; set; }
    }
} 