using Microsoft.EntityFrameworkCore;
using RepairSystem.Domain.Entities;
using RepairSystem.Infrastructure.ReadModels;

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

    public DbSet<ComplaintStatusHistory> ComplaintStatusHistories
    => Set<ComplaintStatusHistory>();

    public DbSet<SystemLogEntry> SystemLogs => Set<SystemLogEntry>();

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

        modelBuilder.Entity<ComplaintStatusHistory>()
    .HasKey(history => history.ComplaintStatusHistoryId);

        modelBuilder.Entity<ComplaintStatusHistory>()
            .HasOne(history => history.Complaint)
            .WithMany()
            .HasForeignKey(history => history.ComplaintId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComplaintStatusHistory>()
            .HasOne(history => history.ChangedByUser)
            .WithMany()
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SystemLogEntry>(entity =>
        {
            entity.HasNoKey();

            entity.ToTable(
                "SystemLogs",
                "dbo",
                table => table.ExcludeFromMigrations());

            entity.Property(log => log.Id)
                .HasColumnName("Id");

            entity.Property(log => log.Message)
                .HasColumnType("nvarchar(max)");

            entity.Property(log => log.MessageTemplate)
                .HasColumnType("nvarchar(max)");

            entity.Property(log => log.Level)
                .HasMaxLength(16);

            entity.Property(log => log.TimeStamp)
                .HasColumnType("datetime");

            entity.Property(log => log.Exception)
                .HasColumnType("nvarchar(max)");

            entity.Property(log => log.Properties)
                .HasColumnType("nvarchar(max)");

            entity.Property(log => log.ActorRole)
                .HasMaxLength(32);

            entity.Property(log => log.RequestMethod)
                .HasMaxLength(16);

            entity.Property(log => log.RequestPath)
                .HasMaxLength(512);

            entity.Property(log => log.Elapsed)
                .HasColumnType("float");

            entity.Property(log => log.CorrelationId)
                .HasMaxLength(64);

            entity.Property(log => log.Action)
                .HasMaxLength(64);

            entity.Property(log => log.ResourceType)
                .HasMaxLength(64);

            entity.Property(log => log.Outcome)
                .HasMaxLength(32);

            entity.Property(log => log.Application)
                .HasMaxLength(64);
        });
    }
}