using System.ComponentModel.DataAnnotations;

namespace SubBot.Database;

public class VanillaApplication
{
    [Key]
    public int Id { get; set; }

    [MaxLength(32)]
    public string ApplicationCode { get; set; } = string.Empty;

    public ulong UserId { get; set; }

    [MaxLength(64)]
    public string MinecraftNick { get; set; } = string.Empty;

    [MaxLength(16)]
    public string Age { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Interest { get; set; } = string.Empty;

    public ulong NotificationsChannelId { get; set; }

    [MaxLength(16)]
    public string Status { get; set; } = "pending";

    public ulong? ReviewedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }
}
