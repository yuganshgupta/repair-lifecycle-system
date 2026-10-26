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

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

        modelBuilder.Entity<RefreshToken>()
    .HasKey(token => token.RefreshTokenId);

        modelBuilder.Entity<RefreshToken>()
            .Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(token => token.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(token => token.FamilyId);

        modelBuilder.Entity<RefreshToken>()
            .Property(token => token.ReplacedByTokenHash)
            .HasMaxLength(64);

        modelBuilder.Entity<RefreshToken>()
            .Property(token => token.RowVersion)
            .IsRowVersion();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}