using Microsoft.EntityFrameworkCore;
using SchedulerApp.Api.Models;

namespace SchedulerApp.Api.Data;

public sealed class SchedulerDbContext(DbContextOptions<SchedulerDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shift>()
            .HasOne<Employee>()
            .WithMany()
            .HasForeignKey(shift => shift.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
