using System;

namespace Gymerp.Domain.Entities
{
    public class Plan
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int DurationInMonths { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public decimal MonthlyValue { get; private set; }
        public int MassageHoursPerMonth { get; private set; }
        public int ExtraActivitiesPerMonth { get; private set; }
        public bool HasUnlimitedClasses { get; private set; }

        protected Plan() { }

        public Plan(string name, string description, decimal price, int durationInMonths)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description;
            Price = price;
            DurationInMonths = durationInMonths;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Update(string name, string description, decimal price, int durationInMonths)
        {
            Name = name;
            Description = description;
            Price = price;
            DurationInMonths = durationInMonths;
            UpdatedAt = DateTime.UtcNow;
        }

        public static Plan CreateGoldPlan()
        {
            return new Plan("Gold", "Gold Plan", 250m, 16);
        }

        public static Plan CreateSilverPlan()
        {
            return new Plan("Silver", "Silver Plan", 230m, 12);
        }
    }
} 