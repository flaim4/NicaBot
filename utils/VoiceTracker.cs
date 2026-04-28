using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SubBot.Database;

namespace SubBot.Utils;

public static class VoiceTracker
{
    public static async Task HandleVoiceStateUpdatedAsync(
        string connectionString,
        SocketUser user,
        SocketVoiceState before,
        SocketVoiceState after)
    {
        if (user is not SocketGuildUser guildUser || guildUser.IsBot)
            return;

        var wasInVoice = before.VoiceChannel != null;
        var isInVoice = after.VoiceChannel != null;

        if (!wasInVoice && !isInVoice)
            return;

        if (wasInVoice && isInVoice)
            return;

        using var db = new BotDbContext(connectionString);
        var dbUser = await DbUserService.GetOrCreateFullUserAsync(db, user.Id);
        var voiceStat = dbUser.VoiceStat!;
        var now = DateTime.UtcNow;

        if (!wasInVoice && isInVoice)
        {
            voiceStat.SessionStartedAtUtc ??= now;
        }
        else if (wasInVoice && !isInVoice)
        {
            FinishVoiceSession(voiceStat, now);
        }

        await db.SaveChangesAsync();
        await LeaderRoleService.UpdateLeaderRolesAsync(guildUser.Guild, db);
    }

    public static async Task SyncActiveVoiceSessionsAsync(DiscordSocketClient client, string connectionString)
    {
        using var db = new BotDbContext(connectionString);
        var now = DateTime.UtcNow;

        var connectedUserIds = client.Guilds
            .SelectMany(guild => guild.Users)
            .Where(user => !user.IsBot && user.VoiceChannel != null)
            .Select(user => user.Id)
            .ToHashSet();

        var activeVoiceStats = await db.UserVoiceStats
            .Where(user => user.SessionStartedAtUtc != null)
            .ToListAsync();

        foreach (var voiceStat in activeVoiceStats)
        {
            if (!connectedUserIds.Contains(voiceStat.UserId))
                FinishVoiceSession(voiceStat, now);
        }

        foreach (var userId in connectedUserIds)
        {
            var dbUser = await DbUserService.GetOrCreateFullUserAsync(db, userId);
            dbUser.VoiceStat!.SessionStartedAtUtc ??= now;
        }

        await db.SaveChangesAsync();

        foreach (var guild in client.Guilds)
            await LeaderRoleService.UpdateLeaderRolesAsync(guild, db);
    }

    public static long GetCurrentVoiceSeconds(UserVoiceStat voiceStat)
    {
        if (voiceStat.SessionStartedAtUtc == null)
            return voiceStat.TotalSeconds;

        var extraSeconds = (long)Math.Max(0, (DateTime.UtcNow - voiceStat.SessionStartedAtUtc.Value).TotalSeconds);
        return voiceStat.TotalSeconds + extraSeconds;
    }

    private static void FinishVoiceSession(UserVoiceStat voiceStat, DateTime finishedAtUtc)
    {
        if (voiceStat.SessionStartedAtUtc == null)
            return;

        var elapsedSeconds = (long)Math.Max(0, (finishedAtUtc - voiceStat.SessionStartedAtUtc.Value).TotalSeconds);
        voiceStat.TotalSeconds += elapsedSeconds;
        voiceStat.SessionStartedAtUtc = null;
    }
}
