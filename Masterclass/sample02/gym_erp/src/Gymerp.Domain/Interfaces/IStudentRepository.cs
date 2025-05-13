using System;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Domain.Interfaces
{
    public interface IStudentRepository
    {
        Task<Student> GetByIdAsync(Guid id);
        Task<Student> GetByDocumentAsync(string document);
        Task AddAsync(Student student);
        Task UpdateAsync(Student student);
    }
} 