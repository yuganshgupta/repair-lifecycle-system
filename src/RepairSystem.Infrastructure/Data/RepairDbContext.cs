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

    public DbSet<BusinessActivityEvent> BusinessActivityEvents
    => Set<BusinessActivityEvent>();


    public DbSet<Quote> Quotes
        => Set<Quote>();

    public DbSet<QuoteItem> QuoteItems
        => Set<QuoteItem>();

    public DbSet<Attachment> Attachments
    => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Complaint>()
            .Property<byte[]>("RowVersion")
            .IsRequired()
            .IsRowVersion();


        modelBuilder.Entity<Quote>(entity =>
        {
            entity.ToTable("Quotes", table =>
            {
                table.HasCheckConstraint(
                    "CK_Quotes_InspectionFee",
                    "[InspectionFee] >= 0");

                table.HasCheckConstraint(
                    "CK_Quotes_EstimatedTotalCost",
                    "[EstimatedTotalCost] >= 0");

                table.HasCheckConstraint(
                    "CK_Quotes_WarrantyCharges",
                    "([IsWarrantyValid] = 1 AND [InspectionFee] = 0 " +
                    "AND [EstimatedTotalCost] = 0) OR " +
                    "([IsWarrantyValid] = 0 AND [InspectionFee] > 0)");

                table.HasCheckConstraint(
                    "CK_Quotes_CustomerDecision",
                    "[CustomerDecision] IN (0, 1, 2)");
            });

            entity.HasKey(x => x.QuoteId);

            entity.Property(x => x.DiagnosticFindings)
                 .HasMaxLength(2000)
                .IsRequired();

            entity.Ignore(x => x.InspectionFeeDue);

            entity.HasIndex(x => x.ComplaintId)
                .IsUnique();

            entity.HasOne(x => x.Complaint)
                .WithOne()
                .HasForeignKey<Quote>(x => x.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Technician)
                .WithMany()
                .HasForeignKey(x => x.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.InspectionFee)
                .HasPrecision(18, 2);

            entity.Property(x => x.EstimatedTotalCost)
                .HasPrecision(18, 2);

            entity.Property(x => x.WarrantyRejectionReason)
                .HasMaxLength(500);

            entity.Property(x => x.DecisionReason)
                .HasMaxLength(500);

            entity.Property(x => x.CustomerDecision)
                .HasConversion<int>();

            entity.Property(x => x.CreatedAt)
                .HasColumnType("datetime2");

            entity.Property(x => x.DecisionTimestamp)
                .HasColumnType("datetime2");

            entity.HasMany(x => x.Items)
                .WithOne(x => x.Quote)
                .HasForeignKey(x => x.QuoteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Navigation(x => x.Items)
                .HasField("_items")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Property<byte[]>("RowVersion")
                .IsRowVersion();
        });

        modelBuilder.Entity<QuoteItem>(entity =>
        {
            entity.ToTable("QuoteItems", table =>
            {
                table.HasCheckConstraint(
                    "CK_QuoteItems_Quantity",
                    "[Quantity] > 0");

                table.HasCheckConstraint(
                    "CK_QuoteItems_UnitPrice",
                    "[UnitPrice] >= 0");

                table.HasCheckConstraint(
                    "CK_QuoteItems_ItemType",
                    "[ItemType] IN (0, 1)");
            });

            entity.HasKey(x => x.QuoteItemId);

            entity.Property(x => x.Description)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.ItemType)
                .HasConversion<int>();

            entity.Ignore(x => x.TotalPrice);
        });


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

        modelBuilder.Entity<BusinessActivityEvent>(entity =>
        {
            entity.ToTable("BusinessActivityEvents", "dbo");

            entity.HasKey(activity => activity.BusinessActivityEventId);

            entity.Property(activity => activity.BusinessActivityEventId)
                .ValueGeneratedOnAdd();

            entity.Property(activity => activity.EventType)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(activity => activity.OccurredAtUtc)
                .HasColumnType("datetime2")
                .IsRequired();

            entity.Property(activity => activity.ActorUserId);

            entity.Property(activity => activity.ActorName)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(activity => activity.ActorRole)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(activity => activity.TargetUserId);

            entity.Property(activity => activity.TargetUserName)
                .HasMaxLength(120);

            entity.Property(activity => activity.CustomerId);

            entity.Property(activity => activity.CustomerName)
                .HasMaxLength(120);

            entity.Property(activity => activity.ComplaintId);

            entity.Property(activity => activity.TicketNumber)
                .HasMaxLength(64);

            entity.Property(activity => activity.Title)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(activity => activity.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(activity => activity.DetailsJson)
                .HasColumnType("nvarchar(max)");

            // Global activity feed: newest events first.
            entity.HasIndex(activity => activity.OccurredAtUtc);

            // Complaint and customer activity timelines.
            entity.HasIndex(activity =>
                new { activity.ComplaintId, activity.OccurredAtUtc });

            entity.HasIndex(activity =>
                new { activity.CustomerId, activity.OccurredAtUtc });

            // Filter activity by the user who performed it.
            entity.HasIndex(activity =>
                new { activity.ActorUserId, activity.OccurredAtUtc });

            // Filter activity by event category.
            entity.HasIndex(activity =>
                new { activity.EventType, activity.OccurredAtUtc });
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.ToTable("Attachments", table =>
            {
                table.HasCheckConstraint(
                    "CK_Attachments_FileSize",
                    "[FileSizeBytes] > 0 AND [FileSizeBytes] <= 20971520");

                table.HasCheckConstraint(
                    "CK_Attachments_Purpose",
                    "[Purpose] IN (0, 1, 2)");
            });

            entity.HasKey(x => x.AttachmentId);

            entity.HasOne(x => x.Complaint)
                .WithMany()
                .HasForeignKey(x => x.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.FilePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.ContentType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Purpose)
                .HasConversion<int>();

            entity.HasIndex(x => new
            {
                x.ComplaintId,
                x.Purpose
            });
        });
    }
}