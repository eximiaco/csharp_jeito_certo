using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;

namespace GymErp.IntegrationTests.Orchestration.NewEnrollmentFlow.TestHelpers;

/// <summary>
/// Builder para criar dados de teste para o NewEnrollmentFlow
/// </summary>
public class TestDataBuilder
{
    private NewEnrollmentFlowData _data;

    public TestDataBuilder()
    {
        _data = new NewEnrollmentFlowData();
    }

    /// <summary>
    /// Cria dados válidos para o workflow NewEnrollment
    /// </summary>
    public static TestDataBuilder CreateValidData()
    {
        return new TestDataBuilder()
            .WithClientId(Guid.NewGuid())
            .WithPlanId(Guid.NewGuid())
            .WithName("João da Silva Santos")
            .WithEmail("joao.silva@email.com")
            .WithPhone("11999999999")
            .WithDocument("52998224725")
            .WithBirthDate(new DateTime(1990, 1, 1))
            .WithGender("M")
            .WithAddress("Rua Exemplo, 123")
            .WithStartDate(DateTime.Now)
            .WithEndDate(DateTime.Now.AddMonths(12))
            .WithPersonalId(Guid.NewGuid())
            .WithAssessmentDate(DateTime.Now.AddDays(7))
            .WithWeight(75.5m)
            .WithHeight(1.75m)
            .WithBodyFatPercentage(15.0m)
            .WithNotes("Cliente iniciante");
    }

    /// <summary>
    /// Cria dados com cliente inválido
    /// </summary>
    public static TestDataBuilder CreateWithInvalidClient()
    {
        return new TestDataBuilder()
            .WithClientId(Guid.Empty) // Cliente inválido
            .WithPlanId(Guid.NewGuid())
            .WithName("João da Silva Santos")
            .WithEmail("joao.silva@email.com")
            .WithPhone("11999999999")
            .WithDocument("52998224725")
            .WithBirthDate(new DateTime(1990, 1, 1))
            .WithGender("M")
            .WithAddress("Rua Exemplo, 123")
            .WithStartDate(DateTime.Now)
            .WithEndDate(DateTime.Now.AddMonths(12))
            .WithPersonalId(Guid.NewGuid())
            .WithAssessmentDate(DateTime.Now.AddDays(7))
            .WithWeight(75.5m)
            .WithHeight(1.75m)
            .WithBodyFatPercentage(15.0m)
            .WithNotes("Cliente iniciante");
    }

    /// <summary>
    /// Cria dados com plano inválido
    /// </summary>
    public static TestDataBuilder CreateWithInvalidPlan()
    {
        return new TestDataBuilder()
            .WithClientId(Guid.NewGuid())
            .WithPlanId(Guid.Empty) // Plano inválido
            .WithName("João da Silva Santos")
            .WithEmail("joao.silva@email.com")
            .WithPhone("11999999999")
            .WithDocument("52998224725")
            .WithBirthDate(new DateTime(1990, 1, 1))
            .WithGender("M")
            .WithAddress("Rua Exemplo, 123")
            .WithStartDate(DateTime.Now)
            .WithEndDate(DateTime.Now.AddMonths(12))
            .WithPersonalId(Guid.NewGuid())
            .WithAssessmentDate(DateTime.Now.AddDays(7))
            .WithWeight(75.5m)
            .WithHeight(1.75m)
            .WithBodyFatPercentage(15.0m)
            .WithNotes("Cliente iniciante");
    }

    /// <summary>
    /// Cria dados para cenário de falha específico
    /// </summary>
    public static TestDataBuilder CreateForFailureScenario(FailingStep failingStep)
    {
        var builder = CreateValidData();
        
        switch (failingStep)
        {
            case FailingStep.AddEnrollment:
                builder.WithEnrollmentCreated(false);
                break;
            case FailingStep.AddLegacyEnrollment:
                builder.WithEnrollmentCreated(true)
                       .WithLegacyEnrollmentCreated(false);
                break;
            case FailingStep.ProcessPayment:
                builder.WithEnrollmentCreated(true)
                       .WithLegacyEnrollmentCreated(true)
                       .WithPaymentProcessed(false);
                break;
            case FailingStep.ScheduleEvaluation:
                builder.WithEnrollmentCreated(true)
                       .WithLegacyEnrollmentCreated(true)
                       .WithPaymentProcessed(true)
                       .WithEvaluationScheduled(false);
                break;
        }
        
        return builder;
    }

    public TestDataBuilder WithClientId(Guid clientId)
    {
        _data.ClientId = clientId;
        return this;
    }

    public TestDataBuilder WithPlanId(Guid planId)
    {
        _data.PlanId = planId;
        return this;
    }

    public TestDataBuilder WithName(string name)
    {
        _data.Name = name;
        return this;
    }

    public TestDataBuilder WithEmail(string email)
    {
        _data.Email = email;
        return this;
    }

    public TestDataBuilder WithPhone(string phone)
    {
        _data.Phone = phone;
        return this;
    }

    public TestDataBuilder WithDocument(string document)
    {
        _data.Document = document;
        return this;
    }

    public TestDataBuilder WithBirthDate(DateTime birthDate)
    {
        _data.BirthDate = birthDate;
        return this;
    }

    public TestDataBuilder WithGender(string gender)
    {
        _data.Gender = gender;
        return this;
    }

    public TestDataBuilder WithAddress(string address)
    {
        _data.Address = address;
        return this;
    }

    public TestDataBuilder WithEnrollmentId(Guid enrollmentId)
    {
        _data.EnrollmentId = enrollmentId;
        return this;
    }

    public TestDataBuilder WithEnrollmentCreated(bool created)
    {
        _data.EnrollmentCreated = created;
        return this;
    }

    public TestDataBuilder WithPaymentProcessed(bool processed)
    {
        _data.PaymentProcessed = processed;
        return this;
    }

    public TestDataBuilder WithEvaluationScheduled(bool scheduled)
    {
        _data.EvaluationScheduled = scheduled;
        return this;
    }

    public TestDataBuilder WithStartDate(DateTime startDate)
    {
        _data.StartDate = startDate;
        return this;
    }

    public TestDataBuilder WithEndDate(DateTime endDate)
    {
        _data.EndDate = endDate;
        return this;
    }

    public TestDataBuilder WithPersonalId(Guid personalId)
    {
        _data.PersonalId = personalId;
        return this;
    }

    public TestDataBuilder WithAssessmentDate(DateTime assessmentDate)
    {
        _data.AssessmentDate = assessmentDate;
        return this;
    }

    public TestDataBuilder WithWeight(decimal weight)
    {
        _data.Weight = weight;
        return this;
    }

    public TestDataBuilder WithHeight(decimal height)
    {
        _data.Height = height;
        return this;
    }

    public TestDataBuilder WithBodyFatPercentage(decimal bodyFatPercentage)
    {
        _data.BodyFatPercentage = bodyFatPercentage;
        return this;
    }

    public TestDataBuilder WithNotes(string notes)
    {
        _data.Notes = notes;
        return this;
    }

    public TestDataBuilder WithLegacyEnrollmentId(Guid legacyEnrollmentId)
    {
        _data.LegacyEnrollmentId = legacyEnrollmentId;
        return this;
    }

    public TestDataBuilder WithLegacyEnrollmentCreated(bool created)
    {
        _data.LegacyEnrollmentCreated = created;
        return this;
    }

    public NewEnrollmentFlowData Build()
    {
        return _data;
    }
}

/// <summary>
/// Enum para identificar qual step deve falhar nos cenários de teste
/// </summary>
public enum FailingStep
{
    AddEnrollment,
    AddLegacyEnrollment,
    ProcessPayment,
    ScheduleEvaluation
}
