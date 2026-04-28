using System.ComponentModel.DataAnnotations;

namespace SubBot.Database;

public class AcceptedApplication
{
    [Key]
    public int Id { get; set; }

    [MaxLength(32)]
    public string ApplicationCode { get; set; } = string.Empty;

    public ulong UserId { get; set; }

    public ulong ReviewedByUserId { get; set; }

    [MaxLength(64)]
    public string MinecraftNick { get; set; } = string.Empty;

    [MaxLength(16)]
    public string Age { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Interest { get; set; } = string.Empty;

    public DateTime AcceptedAtUtc { get; set; }

    public User User { get; set; } = null!;

    public User ReviewedByUser { get; set; } = null!;
}
