using Gymerp.Application.DTOs;
using Gymerp.Application.Interfaces;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;

namespace Gymerp.Application.Services
{
    public class EnrollmentService(
        IStudentRepository studentRepository, 
        IPlanRepository planRepository,
        IEnrollmentRepository enrollmentRepository) : IEnrollmentService
    {
        public async Task<Guid> CreateEnrollmentAsync(EnrollmentDto dto)
        {
            // 1. Verifica se o aluno já existe pelo documento
            var student = await studentRepository.GetByDocumentAsync(dto.Student.Document);
            if (student == null)
            {
                student = new Student(
                    dto.Student.Name,
                    dto.Student.Email,
                    dto.Student.Phone,
                    dto.Student.Document,
                    dto.Student.BirthDate,
                    dto.Student.Gender,
                    dto.Student.Address
                );
                await studentRepository.AddAsync(student);
            }

            // 2. Busca o plano
            var plan = await planRepository.GetByIdAsync(dto.PlanId);
            if (plan == null)
                throw new InvalidOperationException("Plano não encontrado");

            // 3. Cria a matrícula com status Pending
            var enrollment = new Enrollment(student.Id, plan.Id, dto.StartDate, dto.EndDate);
            await enrollmentRepository.AddAsync(enrollment);

            return enrollment.Id;
        }
    }
}
