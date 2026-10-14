using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Entities;

public class Attachment
{
    public int AttachmentId { get; set; }

    public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    public AttachmentPurpose Purpose { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}