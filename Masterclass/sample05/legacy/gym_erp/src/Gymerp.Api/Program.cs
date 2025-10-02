using Gymerp.Application.Interfaces;
using Gymerp.Application.Services;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Gymerp.Infrastructure.Repositories;
using Gymerp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IPhysicalAssessmentRepository, PhysicalAssessmentRepository>();
builder.Services.AddScoped<IPersonalRepository, PersonalRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Register services
builder.Services.AddScoped<IFullEnrollmentService, FullEnrollmentService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IScheduleAssessmentService, ScheduleAssessmentService>();
builder.Services.AddScoped<IProcessPaymentService, ProcessPaymentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
