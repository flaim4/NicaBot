using System.ComponentModel.DataAnnotations;

namespace SubBot.Database;

public class User
{
    [Key]
    public ulong Id { get; set; }

    public int? ClanId { get; set; }

    public UserBalance? Balance { get; set; }

    public UserVoiceStat? VoiceStat { get; set; }

    public Clan? Clan { get; set; }

    public UserMinecraftProfile? MinecraftProfile { get; set; }

    public ICollection<AcceptedApplication> AcceptedApplications { get; set; } = new List<AcceptedApplication>();

    public ICollection<AcceptedApplication> ReviewedAcceptedApplications { get; set; } = new List<AcceptedApplication>();

    public ICollection<ClanInvite> SentClanInvites { get; set; } = new List<ClanInvite>();

    public ICollection<ClanInvite> ReceivedClanInvites { get; set; } = new List<ClanInvite>();

    public ICollection<PendingClanCreation> PendingClanCreations { get; set; } = new List<PendingClanCreation>();
}
