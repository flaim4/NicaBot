using System.ComponentModel.DataAnnotations;

namespace SubBot.Database;

public class Clan
{
    [Key]
    public int Id { get; set; }

    [MaxLength(4)]
    public string Tag { get; set; } = string.Empty;

    public ulong OwnerUserId { get; set; }

    public ulong RoleId { get; set; }

    public ulong ChannelId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime NextPaymentDueAtUtc { get; set; }

    public User OwnerUser { get; set; } = null!;

    public ICollection<User> Members { get; set; } = new List<User>();

    public ICollection<ClanInvite> Invites { get; set; } = new List<ClanInvite>();
}
