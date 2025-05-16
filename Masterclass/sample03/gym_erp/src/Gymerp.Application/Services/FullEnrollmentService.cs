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

        public async Task<Enrollment> EnrollAsync(FullEnrollmentDto dto)
        {
            // 1. Obtém ou cria o aluno
            var student = await GetOrCreateStudentAsync(dto.Student);

            // 2. Cria a matrícula
            var enrollment = await CreateEnrollmentAsync(student, dto.PlanId, dto.StartDate, dto.EndDate);

            // 3. Processa o pagamento
            var paymentResult = await _paymentService.ProcessAsync(enrollment);
            if (!paymentResult.Success)
            {
                throw new InvalidOperationException(paymentResult.Message);
            }

            // 4. Agenda avaliação física apenas se o pagamento for aprovado
            await SchedulePhysicalAssessmentAsync(student, dto.PhysicalAssessment);

            return enrollment;
        }
        
        private async Task<Student> GetOrCreateStudentAsync(StudentDto studentDto)
        {
            var student = await _studentRepository.GetByDocumentAsync(studentDto.Document);
            if (student == null)
            {
                student = new Student(
                    studentDto.Name,
                    studentDto.Email,
                    studentDto.Phone,
                    studentDto.Document,
                    studentDto.BirthDate,
                    studentDto.Gender,
                    studentDto.Address
                );
                await _studentRepository.AddAsync(student);
            }
            return student;
        }

        private async Task<Enrollment> CreateEnrollmentAsync(Student student, Guid planId, DateTime startDate, DateTime endDate)
        {
            var plan = await _planRepository.GetByIdAsync(planId);
            if (plan == null)
                throw new InvalidOperationException("Plano não encontrado");

            var enrollment = new Enrollment(
                student.Id,
                plan.Id,
                startDate,
                endDate
            );
            await _enrollmentRepository.AddAsync(enrollment);
            return enrollment;
        }

        private async Task<PhysicalAssessment> SchedulePhysicalAssessmentAsync(Student student, PhysicalAssessmentDto assessmentDto)
        {
            var personal = await _personalRepository.GetByIdAsync(assessmentDto.PersonalId);
            if (personal == null)
                throw new InvalidOperationException("Personal não encontrado para avaliação física");

            var existingAssessment = await _assessmentRepository.GetByDateAsync(assessmentDto.AssessmentDate);
            if (existingAssessment.Any(a => a.PersonalId == personal.Id && a.Status != PhysicalAssessmentStatus.Cancelled))
                throw new InvalidOperationException("Personal não está disponível neste horário para avaliação física");

            var assessment = new PhysicalAssessment(
                student.Id,
                personal.Id,
                assessmentDto.AssessmentDate,
                assessmentDto.Weight,
                assessmentDto.Height,
                assessmentDto.BodyFatPercentage,
                assessmentDto.Notes
            );
            await _assessmentRepository.AddAsync(assessment);
            return assessment;
        }
    }
} 