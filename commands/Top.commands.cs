using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SubBot.Database;
using SubBot.Utils;

public static class TopCommand
{
    private const int TopPageSize = 16;

    [Slash("топ", "Посмотреть топ пользователей по рубинам")]
    public static async Task Top(SocketSlashCommand cmd)
    {
        try
        {
            await cmd.DeferAsync();

            var guild = (cmd.Channel as SocketGuildChannel)?.Guild;
            if (guild == null)
            {
                await cmd.FollowupAsync("Команда работает только на сервере.");
                return;
            }

            using var db = new BotDbContext(ConfigLoader.GetConnectionString());
            var allUsers = await GetLeaderboard(db, guild);

            if (!allUsers.Any())
            {
                var empty = new ComponentBuilderV2(new[]
                {
                    new ContainerBuilder()
                        .WithTextDisplay(new TextDisplayBuilder()
                            .WithContent(
                                "**Топ по рубинам**\n" +
                                "-# Накопи больше всех и получи почетное звание!\n\n" +
                                "Пока здесь никого нет...\n" +
                                "-# Забирай ежедневный бонус и копи рубины!"
                            ))
                });

                await cmd.FollowupAsync(components: empty.Build(), allowedMentions: AllowedMentions.None);
                return;
            }

            var page = 1;
            var totalPages = (int)Math.Ceiling(allUsers.Count / (double)TopPageSize);
            var usersPage = allUsers.Take(TopPageSize).ToList();

            var text = BuildTopText(usersPage, page);
            var components = BuildTopComponents(text, page, totalPages);

            await cmd.FollowupAsync(components: components.Build(), allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка Top: {ex}");
        }
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
                        "-# Забирай ежедневный бонус и копи рубины!"
                    ))
                .WithActionRow(new ActionRowBuilder()
                    .WithComponents(new[]
                    {
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Назад")
                            .WithCustomId($"top_page_{page - 1}")
                            .WithDisabled(page <= 1)
                            .WithEmote(Emote.Parse("<:left:1482375481918754982>")),
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Далее")
                            .WithCustomId($"top_page_{page + 1}")
                            .WithDisabled(page >= totalPages)
                            .WithEmote(Emote.Parse("<:right:1482375507118133289>"))
                    }))
        });
    }

    [InteractionEvent]
    public static async Task HandleTopPage(SocketMessageComponent comp, BotDbContext db)
    {
        if (!comp.Data.CustomId.StartsWith("top_page_"))
            return;

        try
        {
            var guild = (comp.Channel as SocketGuildChannel)?.Guild;
            if (guild == null)
                return;

            var pageStr = comp.Data.CustomId.Replace("top_page_", string.Empty);
            if (!int.TryParse(pageStr, out var page))
                return;

            var allUsers = await GetLeaderboard(db, guild);
            if (!allUsers.Any())
            {
                await comp.RespondAsync("Топ пуст.", ephemeral: true);
                return;
            }

            var totalPages = (int)Math.Ceiling(allUsers.Count / (double)TopPageSize);
            page = Math.Clamp(page, 1, totalPages);

            var usersPage = allUsers
                .Skip((page - 1) * TopPageSize)
                .Take(TopPageSize)
                .ToList();

            var text = BuildTopText(usersPage, page);
            var components = BuildTopComponents(text, page, totalPages);

            await comp.UpdateAsync(msg =>
            {
                msg.Components = components.Build();
                msg.AllowedMentions = AllowedMentions.None;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка HandleTopPage: {ex}");
        }
    }

    private static async Task<List<UserBalance>> GetLeaderboard(BotDbContext db, SocketGuild guild)
    {
        var dbUsers = await db.UserBalances.ToListAsync();

        return dbUsers
            .Where(entry =>
            {
                var user = guild.GetUser(entry.UserId);
                return user != null && !user.IsBot;
            })
            .OrderByDescending(entry => entry.Coins)
            .ToList();
    }
}
