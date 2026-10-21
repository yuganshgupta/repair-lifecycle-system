using Microsoft.EntityFrameworkCore;
using RepairSystem.Domain.Entities;

namespace RepairSystem.Infrastructure.Data;

public class RepairDbContext : DbContext
{
    public RepairDbContext(
        DbContextOptions<RepairDbContext> options)
        : base(options)
    {
    }

    public DbSet<Complaint> Complaints => Set<Complaint>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasKey(user => user.UserId);

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<Complaint>()
            .HasKey(complaint => complaint.ComplaintId);

        modelBuilder.Entity<Complaint>()
            .HasIndex(complaint => complaint.TicketNumber)
            .IsUnique();

        modelBuilder.Entity<Complaint>()
            .HasOne(complaint => complaint.Customer)
            .WithMany()
            .HasForeignKey(complaint => complaint.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Complaint>()
            .HasOne(complaint => complaint.AssignedTechnician)
            .WithMany()
            .HasForeignKey(complaint => complaint.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}