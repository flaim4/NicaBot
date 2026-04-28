using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SubBot.Database;
using SubBot.Utils;

public static class VanillaWhitelistCommand
{
    private const string VanillaApplyImageUrl = "https://cdn.discordapp.com/attachments/1381952591981707355/1487609920206409859/Frame_37.png?ex=69c9c433&is=69c872b3&hm=ae479b097d5ac1d93762f1096a2c4ee0ffd3e6909782fee09a76ad9bbd7c02d0&";
    private const string AddNickModalId = "vanilla_whitelist_add_nick_modal";
    private const string BulkAddModalId = "vanilla_whitelist_bulk_add_modal";

    [Slash("вайтлист_добавить", "Добавить ник в whitelist")]
    public static async Task AddToWhitelist(SocketSlashCommand cmd)
    {
        if (!IsAdministrator(cmd.User))
        {
            await cmd.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle("Добавление в whitelist")
            .WithCustomId(AddNickModalId)
            .AddTextInput(
                "Ник в игре",
                "minecraft_nick",
                TextInputStyle.Short,
                placeholder: "sk89q",
                required: true,
                maxLength: 64);

        await cmd.RespondWithModalAsync(modal.Build());
    }

    [ModalEvent]
    public static async Task HandleAddNickModal(SocketModal modal, BotDbContext db)
    {
        if (modal.Data.CustomId != AddNickModalId)
            return;

        if (!IsAdministrator(modal.User))
        {
            await modal.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        var fields = modal.Data.Components.ToDictionary(x => x.CustomId, x => x.Value?.Trim() ?? string.Empty);
        var minecraftNick = GetModalValue(fields, "minecraft_nick");
        if (minecraftNick == "Не указано")
        {
            await modal.RespondAsync("Укажите ник в игре.", ephemeral: true);
            return;
        }

        var exists = await db.AcceptedApplications.AnyAsync(x => x.MinecraftNick == minecraftNick)
            || await db.VanillaApplications.AnyAsync(x => x.MinecraftNick == minecraftNick && x.Status == "accepted");
        if (exists)
        {
            await modal.RespondAsync(
                components: BuildStatusComponent(
                    "Ник уже в whitelist",
                    $"Ник {DiscordTextFormatter.EscapeMarkdown(minecraftNick)} уже есть в списке."
                ).Build(),
                ephemeral: true
            );
            return;
        }

        var entry = await AddManualWhitelistEntryAsync(db, modal.User.Id, minecraftNick);
        await db.SaveChangesAsync();

        await modal.RespondAsync(
            components: BuildStatusComponent(
                "Ник добавлен в whitelist",
                $"ID записи: `{entry.Id}`\nНик: {DiscordTextFormatter.EscapeMarkdown(entry.MinecraftNick)}"
            ).Build(),
            ephemeral: true,
            allowedMentions: AllowedMentions.None
        );
    }

    [Slash("вайтлист_добавить_списком", "Добавить в whitelist сразу несколько ников")]
    public static async Task AddManyToWhitelist(SocketSlashCommand cmd)
    {
        if (!IsAdministrator(cmd.User))
        {
            await cmd.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle("Массовое добавление в whitelist")
            .WithCustomId(BulkAddModalId)
            .AddTextInput(
                "Список ников",
                "minecraft_nicks",
                TextInputStyle.Paragraph,
                placeholder: "sk89q\nPlayerTwo\nPlayerThree",
                required: true,
                maxLength: 2000);

        await cmd.RespondWithModalAsync(modal.Build());
    }

    [ModalEvent]
    public static async Task HandleBulkAddModal(SocketModal modal, BotDbContext db)
    {
        if (modal.Data.CustomId != BulkAddModalId)
            return;

        if (!IsAdministrator(modal.User))
        {
            await modal.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        var fields = modal.Data.Components.ToDictionary(x => x.CustomId, x => x.Value?.Trim() ?? string.Empty);
        var rawList = GetModalValue(fields, "minecraft_nicks");
        var entriesToAdd = ParseBulkEntries(rawList);
        if (entriesToAdd.Count == 0)
        {
            await modal.RespondAsync(
                components: BuildStatusComponent(
                    "Не удалось разобрать список",
                    "Укажите ники по одному в каждой строке."
                ).Build(),
                ephemeral: true
            );
            return;
        }

        var targetNicks = entriesToAdd.Select(e => e.MinecraftNick).ToList();
        var existingAcceptedNicks = await db.AcceptedApplications
            .Where(x => targetNicks.Contains(x.MinecraftNick))
            .Select(x => x.MinecraftNick)
            .Distinct()
            .ToListAsync();

        var existingVanillaNicks = await db.VanillaApplications
            .Where(x => targetNicks.Contains(x.MinecraftNick) && x.Status == "accepted")
            .Select(x => x.MinecraftNick)
            .Distinct()
            .ToListAsync();
        var existingNicks = existingAcceptedNicks
            .Concat(existingVanillaNicks)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var addedLines = new List<string>();
        var skippedLines = new List<string>();

        foreach (var bulkEntry in entriesToAdd)
        {
            if (existingNicks.Contains(bulkEntry.MinecraftNick))
            {
                skippedLines.Add(DiscordTextFormatter.EscapeMarkdown(bulkEntry.MinecraftNick) + " - уже есть в whitelist");
                continue;
            }

            var entry = await AddManualWhitelistEntryAsync(db, modal.User.Id, bulkEntry.MinecraftNick);
            existingNicks.Add(bulkEntry.MinecraftNick);
            addedLines.Add($"`{entry.Id}` - {DiscordTextFormatter.EscapeMarkdown(bulkEntry.MinecraftNick)}");
        }

        await db.SaveChangesAsync();

        if (addedLines.Count == 0)
        {
            await modal.RespondAsync(
                components: BuildStatusComponent(
                    "Новые записи не добавлены",
                    string.Join("\n", skippedLines)
                ).Build(),
                ephemeral: true
            );
            return;
        }

        var description = $"Добавлено записей: {addedLines.Count}\n\n{string.Join("\n", addedLines)}";
        if (skippedLines.Count > 0)
            description += $"\n\nПропущено:\n{string.Join("\n", skippedLines)}";

        await modal.RespondAsync(
            components: BuildStatusComponent("Whitelist обновлён", description).Build(),
            ephemeral: true,
            allowedMentions: AllowedMentions.None
        );
    }

    [Slash("вайтлист_список", "Показать список whitelist с ID и никами")]
    public static async Task ListWhitelist(SocketSlashCommand cmd)
    {
        if (!IsAdministrator(cmd.User))
        {
            await cmd.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        await using var db = CreateDbContext();

        var entries = await db.AcceptedApplications
            .OrderByDescending(x => x.AcceptedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(50)
            .ToListAsync();

        if (entries.Count == 0)
        {
            await cmd.RespondAsync(
                components: BuildStatusComponent("Whitelist пуст", "Сейчас в списке нет записей.").Build(),
                ephemeral: true
            );
            return;
        }

        var lines = entries.Select(x =>
            $"`{x.Id}` - {DiscordTextFormatter.EscapeMarkdown(x.MinecraftNick)} - {FormatEntryOwner(x)}");

        await cmd.RespondAsync(
            components: BuildStatusComponent(
                "Список whitelist",
                "ID записи -> ник -> пользователь\n\n" + string.Join("\n", lines)
            ).Build(),
            ephemeral: true,
            allowedMentions: AllowedMentions.None
        );
    }

    [Slash("вайтлист_удалить", "Удалить пользователя из whitelist по ID записи")]
    [SlashOption(
        name: "id",
        description: "ID записи из команды /вайтлист_список",
        type: ApplicationCommandOptionType.Integer,
        required: true
    )]
    public static async Task RemoveFromWhitelist(SocketSlashCommand cmd)
    {
        if (!IsAdministrator(cmd.User))
        {
            await cmd.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        var entryId = GetIntOption(cmd, "id");
        if (entryId <= 0)
        {
            await cmd.RespondAsync("Укажите корректный ID записи.", ephemeral: true);
            return;
        }

        await using var db = CreateDbContext();

        var entry = await db.AcceptedApplications.FirstOrDefaultAsync(x => x.Id == entryId);
        if (entry == null)
        {
            await cmd.RespondAsync(
                components: BuildStatusComponent("Запись не найдена", $"Whitelist запись с ID `{entryId}` не существует.").Build(),
                ephemeral: true
            );
            return;
        }

        var vanillaEntry = await db.VanillaApplications
            .FirstOrDefaultAsync(x => x.ApplicationCode == entry.ApplicationCode);
        if (vanillaEntry != null)
            db.VanillaApplications.Remove(vanillaEntry);

        db.AcceptedApplications.Remove(entry);
        await db.SaveChangesAsync();

        await cmd.RespondAsync(
            components: BuildStatusComponent(
                "Запись удалена",
                $"Удалена запись {FormatEntryOwner(entry)}\nНик: {DiscordTextFormatter.EscapeMarkdown(entry.MinecraftNick)}\nID: `{entry.Id}`"
            ).Build(),
            ephemeral: true,
            allowedMentions: AllowedMentions.None
        );
    }

    private static bool IsAdministrator(IUser user)
        => user is not SocketGuildUser guildUser || guildUser.GuildPermissions.Administrator;

    private static int GetIntOption(SocketSlashCommand cmd, string name)
    {
        var value = cmd.Data.Options.FirstOrDefault(x => x.Name == name)?.Value;
        return value switch
        {
            long longValue => (int)longValue,
            int intValue => intValue,
            _ => 0
        };
    }

    private static string GetModalValue(Dictionary<string, string> fields, string key, string fallback = "Не указано")
    {
        return fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;
    }

    private static BotDbContext CreateDbContext()
        => new(ConfigLoader.GetConnectionString());

    private static ComponentBuilderV2 BuildStatusComponent(string title, string description)
    {
        return new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithMediaGallery(
                    new MediaGalleryBuilder()
                        .AddItems(new MediaGalleryItemProperties(VanillaApplyImageUrl))
                )
                .WithTextDisplay(new TextDisplayBuilder()
                    .WithContent($"**{title}**\n-# {description}"))
        });
    }

    private static List<BulkWhitelistEntry> ParseBulkEntries(string rawList)
    {
        var result = new List<BulkWhitelistEntry>();
        var records = rawList
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var nick = DiscordTextFormatter.NormalizeInput(record);
            if (nick == "Не указано")
                continue;

            result.Add(new BulkWhitelistEntry(nick));
        }

        return result;
    }

    private static string FormatEntryOwner(AcceptedApplication entry)
    {
        return entry.ApplicationCode.StartsWith("manual-", StringComparison.Ordinal)
            ? "ручная запись"
            : DiscordMentionFormatter.User(entry.UserId);
    }

    private static async Task<ulong> GenerateUniqueSyntheticUserIdAsync(BotDbContext db)
    {
        while (true)
        {
            var candidate = unchecked((ulong)Random.Shared.NextInt64(long.MaxValue / 2, long.MaxValue));
            var exists = await db.Users.AnyAsync(x => x.Id == candidate);
            if (!exists)
                return candidate;
        }
    }

    private static async Task<AcceptedApplication> AddManualWhitelistEntryAsync(BotDbContext db, ulong reviewerUserId, string minecraftNick)
    {
        await DbUserService.GetOrCreateFullUserAsync(db, reviewerUserId);

        var applicationCode = await GenerateUniqueManualCodeAsync(db);
        var syntheticUserId = await GenerateUniqueSyntheticUserIdAsync(db);
        await DbUserService.GetOrCreateFullUserAsync(db, syntheticUserId);
        await DbUserService.SetMinecraftNickAsync(db, syntheticUserId, minecraftNick);

        db.VanillaApplications.Add(new VanillaApplication
        {
            ApplicationCode = applicationCode,
            UserId = syntheticUserId,
            MinecraftNick = DiscordTextFormatter.NormalizeInput(minecraftNick),
            Age = "Не указано",
            Interest = "Ручное добавление в whitelist",
            NotificationsChannelId = 0,
            Status = "accepted",
            ReviewedByUserId = reviewerUserId,
            CreatedAtUtc = DateTime.UtcNow,
            ReviewedAtUtc = DateTime.UtcNow
        });

        var entry = new AcceptedApplication
        {
            ApplicationCode = applicationCode,
            UserId = syntheticUserId,
            ReviewedByUserId = reviewerUserId,
            MinecraftNick = DiscordTextFormatter.NormalizeInput(minecraftNick),
            Age = "Не указано",
            Interest = "Ручное добавление в whitelist",
            AcceptedAtUtc = DateTime.UtcNow
        };

        db.AcceptedApplications.Add(entry);
        return entry;
    }

    private static async Task<string> GenerateUniqueManualCodeAsync(BotDbContext db)
    {
        while (true)
        {
            var code = $"manual-{Guid.NewGuid():N}"[..15];
            var exists = await db.AcceptedApplications.AnyAsync(x => x.ApplicationCode == code)
                || await db.VanillaApplications.AnyAsync(x => x.ApplicationCode == code);
            if (!exists)
                return code;
        }
    }

    private sealed record BulkWhitelistEntry(string MinecraftNick);
}
