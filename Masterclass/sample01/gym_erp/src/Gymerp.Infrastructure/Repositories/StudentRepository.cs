using System;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Infrastructure.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly ApplicationDbContext _context;

        public StudentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Student> GetByIdAsync(Guid id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                throw new InvalidOperationException($"Student with ID {id} not found.");
            return student;
        }

        public async Task<Student> GetByDocumentAsync(string document)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Document == document);
            if (student == null)
                throw new InvalidOperationException($"Student with document {document} not found.");
            return student;
        }

        public async Task AddAsync(Student student)
        {
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }
    }
} 