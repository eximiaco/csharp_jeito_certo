using System;
using System.Linq;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;
using Gymerp.Application.Interfaces;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;

namespace Gymerp.Application.Services
{
    public class ScheduleAssessmentService(IPhysicalAssessmentRepository assessmentRepository, IPersonalRepository personalRepository)
        : IScheduleAssessmentService
    {
        public async Task<Guid> ScheduleAssessmentAsync(ScheduleAssessmentDto dto)
        {
            // 1. Verifica se o personal existe
            var personal = await personalRepository.GetByIdAsync(dto.PersonalId);
            if (personal == null)
                throw new InvalidOperationException("Personal não encontrado para avaliação física");

            // 2. Verifica se o personal está disponível na data/hora
            var existingAssessment = await assessmentRepository.GetByDateAsync(dto.AssessmentDate);
            if (existingAssessment.Any(a => a.PersonalId == personal.Id && a.Status != PhysicalAssessmentStatus.Cancelled))
                throw new InvalidOperationException("Personal não está disponível neste horário para avaliação física");

            // 3. Cria a avaliação física
            var assessment = new PhysicalAssessment(
                dto.StudentId, 
                dto.PersonalId, 
                dto.AssessmentDate,
                dto.Weight, 
                dto.Height, 
                dto.BodyFatPercentage, 
                dto.Notes
            );
            
            await assessmentRepository.AddAsync(assessment);

            return assessment.Id;
        }
    }
}
