using System.ComponentModel.DataAnnotations;

namespace SubBot.Database;

public class PendingClanCreation
{
    [Key]
    public int Id { get; set; }

    public ulong UserId { get; set; }

    [MaxLength(4)]
    public string Tag { get; set; } = string.Empty;

    [MaxLength(64)]
    public string SecretKey { get; set; } = string.Empty;

    public long Cost { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public bool Consumed { get; set; }

    public User User { get; set; } = null!;
}
