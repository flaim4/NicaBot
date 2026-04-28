using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubBot.Database;

public class UserVoiceStat
{
    [Key]
    [ForeignKey(nameof(User))]
    public ulong UserId { get; set; }

    public long TotalSeconds { get; set; } = 0;

    public DateTime? SessionStartedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
