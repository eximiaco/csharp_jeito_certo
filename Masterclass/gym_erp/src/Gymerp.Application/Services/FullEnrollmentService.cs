using System;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;
using Gymerp.Application.Interfaces;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;

namespace Gymerp.Application.Services
{
    public class FullEnrollmentService : IFullEnrollmentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IPhysicalAssessmentRepository _assessmentRepository;
        private readonly IPersonalRepository _personalRepository;
        private readonly IPaymentService _paymentService;

        public FullEnrollmentService(
            IStudentRepository studentRepository,
            IPlanRepository planRepository,
            IEnrollmentRepository enrollmentRepository,
            IPhysicalAssessmentRepository assessmentRepository,
            IPersonalRepository personalRepository,
            IPaymentService paymentService)
        {
            _studentRepository = studentRepository;
            _planRepository = planRepository;
            _enrollmentRepository = enrollmentRepository;
            _assessmentRepository = assessmentRepository;
            _personalRepository = personalRepository;
            _paymentService = paymentService;
        }

        public async Task<Guid> EnrollAsync(FullEnrollmentDto dto)
        {
            // 1. Verifica se o aluno já existe pelo documento
            var student = await _studentRepository.GetByDocumentAsync(dto.Student.Document);
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
                await _studentRepository.AddAsync(student);
            }

            // 2. Busca o plano
            var plan = await _planRepository.GetByIdAsync(dto.PlanId);
            if (plan == null)
                throw new InvalidOperationException("Plano não encontrado");

            // 3. Cria a matrícula
            var enrollment = new Enrollment(
                student.Id,
                plan.Id,
                dto.StartDate,
                dto.EndDate
            );
            await _enrollmentRepository.AddAsync(enrollment);

            // 4. Processa o pagamento
            var paymentResult = await _paymentService.ProcessAsync(enrollment);
            if (!paymentResult.Success)
            {
                throw new InvalidOperationException(paymentResult.Message);
            }

            // 5. Agenda avaliação física apenas se o pagamento for aprovado
            var personal = await _personalRepository.GetByIdAsync(dto.PhysicalAssessment.PersonalId);
            if (personal == null)
                throw new InvalidOperationException("Personal não encontrado para avaliação física");

            var existingAssessment = await _assessmentRepository.GetByDateAsync(dto.PhysicalAssessment.AssessmentDate);
            if (existingAssessment.Any(a => a.PersonalId == personal.Id && a.Status != PhysicalAssessmentStatus.Cancelled))
                throw new InvalidOperationException("Personal não está disponível neste horário para avaliação física");

            var assessment = new PhysicalAssessment(
                student.Id,
                personal.Id,
                dto.PhysicalAssessment.AssessmentDate,
                dto.PhysicalAssessment.Weight,
                dto.PhysicalAssessment.Height,
                dto.PhysicalAssessment.BodyFatPercentage,
                dto.PhysicalAssessment.Notes
            );
            await _assessmentRepository.AddAsync(assessment);

            return enrollment.Id;
        }
    }
} 