namespace MMS.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Relationships
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<Series> Series { get; set; } = new List<Series>();
    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<BoardVote> BoardVotes { get; set; } = new List<BoardVote>();

    // Mangaka - Assistant Self-Referencing Relationship
    public Guid? MangakaId { get; set; }
    public User? Mangaka { get; set; }
    public ICollection<User> Assistants { get; set; } = new List<User>();
    
    public decimal BaseSalary { get; set; } = 0;
}
