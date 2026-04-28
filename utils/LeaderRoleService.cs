using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SubBot.Database;

namespace SubBot.Utils;

public static class LeaderRoleService
{
    private const int LeaderPlaceCount = 3;

    public static async Task UpdateLeaderRolesAsync(SocketGuild guild, BotDbContext db)
    {
        var config = ConfigLoader.Load().LeaderRoles;
        var rubyRoleId = config.RubyLeaderRoleId;
        var voiceRoleId = config.VoiceLeaderRoleId;

        var rubyLeaderIds = await GetRubyLeaderIdsAsync(guild, db);
        var voiceLeaderIds = await GetVoiceLeaderIdsAsync(guild, db);

        if (rubyRoleId != 0)
            await ApplyLeaderRoleAsync(guild, rubyRoleId, rubyLeaderIds);

        if (voiceRoleId != 0)
            await ApplyLeaderRoleAsync(guild, voiceRoleId, voiceLeaderIds);
    }

    private static async Task<HashSet<ulong>> GetRubyLeaderIdsAsync(SocketGuild guild, BotDbContext db)
    {
        var memberIds = guild.Users
            .Where(x => !x.IsBot)
            .Select(x => x.Id)
            .ToHashSet();

        if (memberIds.Count == 0)
            return new HashSet<ulong>();

        var balances = await db.UserBalances
            .AsNoTracking()
            .Where(x => memberIds.Contains(x.UserId))
            .ToListAsync();

        return balances
            .OrderByDescending(x => x.Coins)
            .ThenBy(x => x.UserId)
            .Take(LeaderPlaceCount)
            .Select(x => x.UserId)
            .ToHashSet();
    }

    private static async Task<HashSet<ulong>> GetVoiceLeaderIdsAsync(SocketGuild guild, BotDbContext db)
    {
        var memberIds = guild.Users
            .Where(x => !x.IsBot)
            .Select(x => x.Id)
            .ToHashSet();

        if (memberIds.Count == 0)
            return new HashSet<ulong>();

        var voiceStats = await db.UserVoiceStats
            .AsNoTracking()
            .Where(x => memberIds.Contains(x.UserId))
            .ToListAsync();

        return voiceStats
            .Select(x => new
            {
                x.UserId,
                VoiceSeconds = VoiceTracker.GetCurrentVoiceSeconds(x)
            })
            .Where(x => x.VoiceSeconds > 0)
            .OrderByDescending(x => x.VoiceSeconds)
            .ThenBy(x => x.UserId)
            .Take(LeaderPlaceCount)
            .Select(x => x.UserId)
            .ToHashSet();
    }

    private static async Task ApplyLeaderRoleAsync(SocketGuild guild, ulong roleId, HashSet<ulong> leaderIds)
    {
        var role = guild.GetRole(roleId);
        if (role == null)
        {
            Logger.Warn($"Роль лидера не найдена на сервере {guild.Id}: {roleId}");
            return;
        }

        var roleHolders = guild.Users
            .Where(x => !x.IsBot && x.Roles.Any(r => r.Id == roleId))
            .ToList();

        foreach (var holder in roleHolders)
        {
            if (leaderIds.Contains(holder.Id))
                continue;

            try
            {
                await holder.RemoveRoleAsync(role);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Не удалось снять роль {roleId} у {holder.Id} на сервере {guild.Id}: {ex.Message}");
            }
        }

        foreach (var leaderId in leaderIds)
        {
            var leader = guild.GetUser(leaderId);
            if (leader == null || leader.IsBot || leader.Roles.Any(r => r.Id == roleId))
                continue;

            try
            {
                await leader.AddRoleAsync(role);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Не удалось выдать роль {roleId} пользователю {leader.Id} на сервере {guild.Id}: {ex.Message}");
            }
        }
    }
}
