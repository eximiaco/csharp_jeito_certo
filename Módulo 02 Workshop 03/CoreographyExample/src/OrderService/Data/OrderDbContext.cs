using System;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions options) : base(options)
    {
    }

    protected OrderDbContext()
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get;set; }
}
