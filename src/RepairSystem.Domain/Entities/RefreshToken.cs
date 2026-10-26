namespace RepairSystem.Domain.Entities;

public class RefreshToken
{
    public int RefreshTokenId { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    // Shared by every rotated token belonging to one login session.
    public Guid FamilyId { get; set; }

    // Store the SHA-256 hash, never the raw refresh token.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Sliding inactivity expiry.
    public DateTime ExpiresAtUtc { get; set; }

    // Hard limit shared by all rotations in this session.
    public DateTime AbsoluteExpiresAtUtc { get; set; }

    // Set when the token is consumed or revoked.
    public DateTime? RevokedAtUtc { get; set; }

    // A rotated token points to the replacement token hash.
    public string? ReplacedByTokenHash { get; set; }

    // Helps prevent concurrent requests from consuming the same token.
    public byte[] RowVersion { get; set; } = [];
}