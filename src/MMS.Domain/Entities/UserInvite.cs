namespace MMS.Domain.Entities;

public class UserInvite
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; } // Added
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid InvitedById { get; set; }
    public User InvitedBy { get; set; } = null!;
}
