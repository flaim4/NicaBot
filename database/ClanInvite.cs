using System.ComponentModel.DataAnnotations;

namespace SubBot.Database;

public class ClanInvite
{
    [Key]
    public int Id { get; set; }

    public int ClanId { get; set; }

    public ulong InviterUserId { get; set; }

    public ulong InvitedUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Clan Clan { get; set; } = null!;

    public User InviterUser { get; set; } = null!;

    public User InvitedUser { get; set; } = null!;
}
