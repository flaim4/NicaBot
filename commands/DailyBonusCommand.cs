using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SubBot.Database;
using SubBot.Utils;

public static class DailyBonusCommand
{
    private const int CooldownHours = 5;
    private const int TopPageSize = 16;

    [Slash("ежедневный_бонус", "Отправить панель ежедневного бонуса")]
    [SlashOption(
        name: "канал",
        description: "Текстовый канал",
        type: ApplicationCommandOptionType.Channel,
        required: true
    )]
    public static async Task DailyBonus(SocketSlashCommand cmd)
    {
        if (cmd.User is SocketGuildUser guildUser && !guildUser.GuildPermissions.Administrator)
        {
            await cmd.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        var channelOption = cmd.Data.Options.FirstOrDefault(x => x.Name == "канал");
        if (channelOption?.Value is not SocketTextChannel targetChannel)
        {
            await cmd.RespondAsync("Канал не найден.", ephemeral: true);
            return;
        }

        var components = new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithMediaGallery(
                    new MediaGalleryBuilder()
                        .AddItems(new MediaGalleryItemProperties(
                            "https://cdn.discordapp.com/attachments/1477995817255309403/1482425740971610345/image.png?ex=69b6e80e&is=69b5968e&hm=87fde58701391200069e88f0a45cfbe6e363d6f3e0eca612e0f7e035b72a631f&"
                        ))
                )
                .WithTextDisplay(
                    new TextDisplayBuilder()
                        .WithContent(
                            "Каждый день вы можете получить случайную награду от 200 до 500 рубинов.\n" +
                            "-# Перезарядка: 5 часов."
                        )
                )
                .WithActionRow(
                    new ActionRowBuilder()
                        .WithComponents(new[]
                        {
                            new ButtonBuilder()
                                .WithStyle(ButtonStyle.Secondary)
                                .WithLabel("Забрать бонус")
                                .WithCustomId("daily_bonus_claim")
                                .WithEmote(Emote.Parse("<:ruby:1482366823159824545>")),
                            new ButtonBuilder()
                                .WithStyle(ButtonStyle.Secondary)
                                .WithLabel("Топ по рубинам")
                                .WithCustomId("daily_bonus_top")
                                .WithEmote(Emote.Parse("<:1_:1482372264086470666>")),
                            new ButtonBuilder()
                                .WithStyle(ButtonStyle.Link)
                                .WithLabel("Поддержка")
                                .WithUrl("https://github.com/flaim4")
                        })
                )
        });

        await targetChannel.SendMessageAsync(components: components.Build(), allowedMentions: AllowedMentions.None);
        await cmd.RespondAsync($"Панель бонуса отправлена в {targetChannel.Mention}", ephemeral: true);
    }

    [InteractionEvent]
    public static async Task HandleDailyBonus(SocketMessageComponent comp, BotDbContext db)
    {
        if (comp.Data.CustomId != "daily_bonus_claim")
            return;

        try
        {
            var userId = comp.User.Id;
            var now = DateTime.UtcNow;

            if (DailyBonusCache.TryGet(userId, out var lastClaimTime))
            {
                var nextClaimTime = lastClaimTime.AddHours(CooldownHours);
                if (now < nextClaimTime)
                {
                    var unix = ((DateTimeOffset)nextClaimTime).ToUnixTimeSeconds();
                    var cooldownComponents = new ComponentBuilderV2(new[]
                    {
                        new ContainerBuilder()
                            .WithTextDisplay(new TextDisplayBuilder()
                                .WithContent(
                                    $"**Бонус на перезарядке**\n" +
                                    $"-# Следующий бонус будет доступен <t:{unix}:R>\n\n" +
                                    "Не забывай заходить каждый день и забирать свою награду!"
                                ))
                    });

                    await comp.RespondAsync(components: cooldownComponents.Build(), ephemeral: true);
                    return;
                }
            }

            var user = await DbUserService.GetOrCreateFullUserAsync(db, userId);

            var coins = Random.Shared.Next(200, 501);
            user.Balance!.Coins += coins;

            await db.SaveChangesAsync();

            var guild = (comp.Channel as SocketGuildChannel)?.Guild;
            if (guild != null)
                await LeaderRoleService.UpdateLeaderRolesAsync(guild, db);

            DailyBonusCache.Set(userId, now);

            var successComponents = new ComponentBuilderV2(new[]
            {
                new ContainerBuilder()
                    .WithTextDisplay(new TextDisplayBuilder()
                        .WithContent(
                            $"**Бонус получен!**\n" +
                            "-# Отличная работа! Продолжай в том же духе!\n\n" +
                            $"Вы получили **{coins}** <:ruby:1482366823159824545>\n" +
                            $"Ваш баланс: **{user.Balance!.Coins}** <:ruby:1482366823159824545>\n\n" +
                            "Возвращайся через 5 часов за новым бонусом!"
                        ))
            });

            await comp.RespondAsync(components: successComponents.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в HandleDailyBonus: {ex.Message}");
            await comp.RespondAsync("Произошла ошибка при получении бонуса.", ephemeral: true);
        }
    }

    [InteractionEvent]
    public static async Task HandleTop(SocketMessageComponent comp, BotDbContext db)
    {
        if (!comp.Data.CustomId.StartsWith("daily_bonus_top"))
            return;

        try
        {
            if (comp.Data.CustomId == "daily_bonus_top")
            {
                await ShowTop(comp, db, 1, isUpdate: false);
                return;
            }

            if (!comp.Data.CustomId.StartsWith("daily_bonus_top_page_"))
                return;

            var pageToken = comp.Data.CustomId.Replace("daily_bonus_top_page_", string.Empty);
            if (!int.TryParse(pageToken, out var page))
                return;

            await ShowTop(comp, db, page, isUpdate: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в HandleTop: {ex.Message}");
            await comp.RespondAsync("Произошла ошибка при обработке команды.", ephemeral: true);
        }
    }

    private static async Task ShowTop(SocketMessageComponent comp, BotDbContext db, int page, bool isUpdate)
    {
        var guild = (comp.Channel as SocketGuildChannel)?.Guild;
        if (guild == null)
            return;

        var dbUsers = await db.UserBalances.ToListAsync();
        var allUsers = dbUsers
            .Where(entry =>
            {
                var user = guild.GetUser(entry.UserId);
                return user != null && !user.IsBot;
            })
            .OrderByDescending(entry => entry.Coins)
            .ToList();

        if (!allUsers.Any())
        {
            await comp.RespondAsync("Топ пуст.", ephemeral: true);
            return;
        }

        var totalPages = (int)Math.Ceiling(allUsers.Count / (double)TopPageSize);
        page = Math.Clamp(page, 1, totalPages);

        var usersPage = allUsers.Skip((page - 1) * TopPageSize).Take(TopPageSize).ToList();
        var text = BuildTopText(usersPage, page);
        var components = BuildTopComponents(text, page, totalPages);

        if (!isUpdate)
        {
            await comp.RespondAsync(components: components.Build(), ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await comp.UpdateAsync(msg =>
        {
            msg.Components = components.Build();
            msg.AllowedMentions = AllowedMentions.None;
        });
    }

    private static string BuildTopText(List<UserBalance> usersPage, int page)
    {
        var text = string.Empty;
        var place = (page - 1) * TopPageSize + 1;

        foreach (var entry in usersPage)
        {
            var mention = DiscordMentionFormatter.User(entry.UserId);
            text += place switch
            {
                1 => $"<:1_:1482372264086470666> {mention} — {entry.Coins} <:ruby:1482366823159824545>\n",
                2 => $"<:2_:1482372314837549098> {mention} — {entry.Coins} <:ruby:1482366823159824545>\n",
                3 => $"<:3_:1482372335418736670> {mention} — {entry.Coins} <:ruby:1482366823159824545>\n",
                _ => $"**{place}.** {mention} — {entry.Coins} <:ruby:1482366823159824545>\n"
            };

            place++;
        }

        return text;
    }

    private static ComponentBuilderV2 BuildTopComponents(string text, int page, int totalPages)
    {
        return new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithTextDisplay(new TextDisplayBuilder()
                    .WithContent(
                        $"**Топ по рубинам (страница {page}/{totalPages})**\n" +
                        "-# Накопи больше всех и получи почетное звание!\n\n" +
                        $"{text}\n" +
                        "-# Хочешь попасть в топ? Забирай ежедневный бонус и копи рубины!"
                    ))
                .WithActionRow(new ActionRowBuilder()
                    .WithComponents(new[]
                    {
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Назад")
                            .WithCustomId($"daily_bonus_top_page_{page - 1}")
                            .WithDisabled(page <= 1)
                            .WithEmote(Emote.Parse("<:left:1482375481918754982>")),
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Далее")
                            .WithCustomId($"daily_bonus_top_page_{page + 1}")
                            .WithDisabled(page >= totalPages)
                            .WithEmote(Emote.Parse("<:right:1482375507118133289>"))
                    }))
        });
    }
}

public static class DailyBonusCache
{
    private static readonly Dictionary<ulong, DateTime> Cache = new();

    public static bool TryGet(ulong userId, out DateTime time)
        => Cache.TryGetValue(userId, out time);

    public static void Set(ulong userId, DateTime time)
        => Cache[userId] = time;
}
