using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubBot.Database;

public class UserBalance
{
    [Key]
    [ForeignKey(nameof(User))]
    public ulong UserId { get; set; }

    public long Coins { get; set; } = 0;

    public User User { get; set; } = null!;
}
