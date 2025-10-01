using Microsoft.EntityFrameworkCore;
using Gymerp.Domain.Entities;

namespace Gymerp.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Personal> Personals { get; set; }
        public DbSet<PhysicalAssessment> PhysicalAssessments { get; set; }
        public DbSet<AccessRecord> AccessRecords { get; set; }
        public DbSet<ScheduledClass> ScheduledClasses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Phone).IsRequired();
                entity.Property(e => e.Document).IsRequired();
                entity.Property(e => e.BirthDate).IsRequired();
                entity.Property(e => e.Gender).IsRequired();
                entity.Property(e => e.Address).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StudentId).IsRequired();
                entity.Property(e => e.PlanId).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();

                entity.HasOne(e => e.Student)
                    .WithMany(s => s.Enrollments)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Plan)
                    .WithMany()
                    .HasForeignKey(e => e.PlanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Plan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Price).IsRequired();
                entity.Property(e => e.DurationInMonths).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EnrollmentId).IsRequired();
                entity.Property(e => e.Amount).IsRequired();
                entity.Property(e => e.DueDate).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.Enrollment)
                    .WithMany(e => e.Payments)
                    .HasForeignKey(e => e.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Personal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Phone).IsRequired();
                entity.Property(e => e.Document).IsRequired();
                entity.Property(e => e.Specialization).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            modelBuilder.Entity<PhysicalAssessment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StudentId).IsRequired();
                entity.Property(e => e.PersonalId).IsRequired();
                entity.Property(e => e.AssessmentDate).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Personal)
                    .WithMany()
                    .HasForeignKey(e => e.PersonalId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ScheduledClass>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EnrollmentId).IsRequired();
                entity.Property(e => e.ScheduledTime).IsRequired();
                entity.Property(e => e.ClassName).IsRequired();

                entity.HasOne(e => e.Enrollment)
                    .WithMany()
                    .HasForeignKey(e => e.EnrollmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
} 