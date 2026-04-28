using System.ComponentModel.DataAnnotations;

namespace SubBot.Database;

public class UserMinecraftProfile
{
    [Key]
    public ulong UserId { get; set; }

    [MaxLength(64)]
    public string MinecraftNick { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
