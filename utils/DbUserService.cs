using Microsoft.EntityFrameworkCore;
using SubBot.Database;

namespace SubBot.Utils;

public static class DbUserService
{
    public static async Task<User> GetOrCreateFullUserAsync(BotDbContext db, ulong userId)
    {
        var trackedUser = db.Users.Local.FirstOrDefault(x => x.Id == userId);
        if (trackedUser != null)
        {
            trackedUser.Balance ??= new UserBalance { UserId = userId };
            trackedUser.VoiceStat ??= new UserVoiceStat { UserId = userId };
            return trackedUser;
        }

        var user = await db.Users
            .Include(x => x.Balance)
            .Include(x => x.VoiceStat)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user != null)
        {
            user.Balance ??= new UserBalance { UserId = userId };
            user.VoiceStat ??= new UserVoiceStat { UserId = userId };
            return user;
        }

        user = new User
        {
            Id = userId,
            Balance = new UserBalance { UserId = userId },
            VoiceStat = new UserVoiceStat { UserId = userId }
        };

        db.Users.Add(user);
        return user;
    }

    public static async Task<UserMinecraftProfile> SetMinecraftNickAsync(BotDbContext db, ulong userId, string minecraftNick)
    {
        var normalizedNick = DiscordTextFormatter.NormalizeInput(minecraftNick);
        var existingByNick = await db.UserMinecraftProfiles.FirstOrDefaultAsync(x => x.MinecraftNick == normalizedNick && x.UserId != userId);
        if (existingByNick != null)
            throw new InvalidOperationException($"Ник {normalizedNick} уже привязан к другому пользователю.");

        var profile = await db.UserMinecraftProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (profile == null)
        {
            profile = new UserMinecraftProfile
            {
                UserId = userId,
                MinecraftNick = normalizedNick,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.UserMinecraftProfiles.Add(profile);
            return profile;
        }

        profile.MinecraftNick = normalizedNick;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        return profile;
    }
}
