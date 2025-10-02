using GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;
using GymErp.Domain.Subscriptions.Aggreates.Plans;

namespace GymErp.IntegrationTests.Orchestration.EnrollmentOrchestrator.TestHelpers;

public class TestDataBuilder
{
    private Request _request;

    public TestDataBuilder()
    {
        _request = new Request
        {
            ClientId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(31),
            Student = new StudentDto
            {
                Name = "João da Silva Santos",
                Email = "joao.silva@email.com",
                Phone = "11999999999",
                Document = "12345678901",
                BirthDate = new DateTime(1990, 1, 1),
                Gender = "M",
                Address = "Rua das Flores, 123"
            },
            PhysicalAssessment = new PhysicalAssessmentDto
            {
                PersonalId = Guid.NewGuid(),
                AssessmentDate = DateTime.UtcNow,
                Weight = 75.5m,
                Height = 175.0m,
                BodyFatPercentage = 15.0m,
                Notes = "Avaliação física inicial"
            }
        };
    }

    public static TestDataBuilder CreateValidRequest()
    {
        return new TestDataBuilder();
    }

    public static TestDataBuilder CreateWithMensalPlan()
    {
        return new TestDataBuilder()
            .WithPlanId(Guid.NewGuid())
            .WithEndDate(DateTime.UtcNow.AddDays(31)); // Mensal = 31 dias
    }

    public static TestDataBuilder CreateWithSemestralPlan()
    {
        return new TestDataBuilder()
            .WithPlanId(Guid.NewGuid())
            .WithEndDate(DateTime.UtcNow.AddDays(180)); // Semestral = ~6 meses
    }

    public static TestDataBuilder CreateWithAnualPlan()
    {
        return new TestDataBuilder()
            .WithPlanId(Guid.NewGuid())
            .WithEndDate(DateTime.UtcNow.AddDays(365)); // Anual = 1 ano
    }

    public static TestDataBuilder CreateWithInvalidPlan()
    {
        return new TestDataBuilder()
            .WithPlanId(Guid.Empty);
    }

    public TestDataBuilder WithClientId(Guid clientId)
    {
        _request.ClientId = clientId;
        return this;
    }

    public TestDataBuilder WithPlanId(Guid planId)
    {
        _request.PlanId = planId;
        return this;
    }

    public TestDataBuilder WithStartDate(DateTime startDate)
    {
        _request.StartDate = startDate;
        return this;
    }

    public TestDataBuilder WithEndDate(DateTime endDate)
    {
        _request.EndDate = endDate;
        return this;
    }

    public TestDataBuilder WithStudentName(string name)
    {
        _request.Student.Name = name;
        return this;
    }

    public TestDataBuilder WithStudentEmail(string email)
    {
        _request.Student.Email = email;
        return this;
    }

    public TestDataBuilder WithStudentPhone(string phone)
    {
        _request.Student.Phone = phone;
        return this;
    }

    public TestDataBuilder WithStudentDocument(string document)
    {
        _request.Student.Document = document;
        return this;
    }

    public TestDataBuilder WithStudentBirthDate(DateTime birthDate)
    {
        _request.Student.BirthDate = birthDate;
        return this;
    }

    public TestDataBuilder WithStudentGender(string gender)
    {
        _request.Student.Gender = gender;
        return this;
    }

    public TestDataBuilder WithStudentAddress(string address)
    {
        _request.Student.Address = address;
        return this;
    }

    public TestDataBuilder WithPhysicalAssessmentPersonalId(Guid personalId)
    {
        _request.PhysicalAssessment.PersonalId = personalId;
        return this;
    }

    public TestDataBuilder WithPhysicalAssessmentDate(DateTime assessmentDate)
    {
        _request.PhysicalAssessment.AssessmentDate = assessmentDate;
        return this;
    }

    public TestDataBuilder WithPhysicalAssessmentWeight(decimal weight)
    {
        _request.PhysicalAssessment.Weight = weight;
        return this;
    }

    public TestDataBuilder WithPhysicalAssessmentHeight(decimal height)
    {
        _request.PhysicalAssessment.Height = height;
        return this;
    }

    public TestDataBuilder WithPhysicalAssessmentBodyFatPercentage(decimal bodyFatPercentage)
    {
        _request.PhysicalAssessment.BodyFatPercentage = bodyFatPercentage;
        return this;
    }

    public TestDataBuilder WithPhysicalAssessmentNotes(string notes)
    {
        _request.PhysicalAssessment.Notes = notes;
        return this;
    }

    public Request Build()
    {
        return _request;
    }
}

public static class PlanInfoBuilder
{
    public static PlanInfo CreateMensalPlan(Guid planId)
    {
        return new PlanInfo(planId, "Plano Mensal", PlanType.Mensal);
    }

    public static PlanInfo CreateSemestralPlan(Guid planId)
    {
        return new PlanInfo(planId, "Plano Semestral", PlanType.Semestral);
    }

    public static PlanInfo CreateAnualPlan(Guid planId)
    {
        return new PlanInfo(planId, "Plano Anual", PlanType.Anual);
    }
}
