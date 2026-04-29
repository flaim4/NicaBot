using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SubBot.Database;
using SubBot.Utils;

public static class VoiceTopCommand
{
    private const int TopPageSize = 16;

    [Slash("топ_войс", "Посмотреть топ пользователей по времени в голосовых каналах")]
    public static async Task VoiceTop(SocketSlashCommand cmd)
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
            var allUsers = await GetLeaderboardAsync(db, guild);

            if (!allUsers.Any())
            {
                var empty = new ComponentBuilderV2(new[]
                {
                    new ContainerBuilder()
                        .WithTextDisplay(new TextDisplayBuilder()
                            .WithContent(
                                "**Топ по войсу**\n" +
                                "-# Здесь показывается, кто больше всего времени провел в голосовых каналах.\n\n" +
                                "Пока здесь никого нет...\n" +
                                "-# Зайди в голосовой канал, и бот начнет учитывать время."
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
            Console.WriteLine($"Ошибка VoiceTop: {ex}");
        }
    }

    [InteractionEvent]
    public static async Task HandleVoiceTopPage(SocketMessageComponent comp, BotDbContext db)
    {
        if (!comp.Data.CustomId.StartsWith("voice_top_page_") && !comp.Data.CustomId.StartsWith("voice_top_refresh_"))
            return;

        try
        {
            var guild = (comp.Channel as SocketGuildChannel)?.Guild;
            if (guild == null)
                return;

            var isRefresh = comp.Data.CustomId.StartsWith("voice_top_refresh_");
            var pageStr = comp.Data.CustomId.Replace(isRefresh ? "voice_top_refresh_" : "voice_top_page_", string.Empty);
            if (!int.TryParse(pageStr, out var page))
                return;

            var allUsers = await GetLeaderboardAsync(db, guild);
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
            Console.WriteLine($"Ошибка HandleVoiceTopPage: {ex}");
        }
    }

    private static async Task<List<UserVoiceStat>> GetLeaderboardAsync(BotDbContext db, SocketGuild guild)
    {
        var dbUsers = await db.UserVoiceStats.ToListAsync();

        return dbUsers
            .Where(entry =>
            {
                var user = guild.GetUser(entry.UserId);
                return user != null && !user.IsBot && VoiceTracker.GetCurrentVoiceSeconds(entry) > 0;
            })
            .OrderByDescending(VoiceTracker.GetCurrentVoiceSeconds)
            .ToList();
    }

    private static string BuildTopText(List<UserVoiceStat> usersPage, int page)
    {
        var text = string.Empty;
        var place = (page - 1) * TopPageSize + 1;

        foreach (var entry in usersPage)
        {
            var mention = DiscordMentionFormatter.User(entry.UserId);
            var voiceTime = VoiceTimeFormatter.Format(VoiceTracker.GetCurrentVoiceSeconds(entry));

            text += place switch
            {
                1 => $"<:1_:1482372264086470666> {mention} — {voiceTime}\n",
                2 => $"<:2_:1482372314837549098> {mention} — {voiceTime}\n",
                3 => $"<:3_:1482372335418736670> {mention} — {voiceTime}\n",
                _ => $"**{place}.** {mention} — {voiceTime}\n"
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
                        $"**Топ по войсу (страница {page}/{totalPages})**\n" +
                        "-# Здесь показывается суммарное время, которое пользователи провели в голосовых каналах.\n\n" +
                        $"{text}\n" +
                        "-# Время обновляется, когда пользователь заходит и выходит из войса."
                    ))
                .WithActionRow(new ActionRowBuilder()
                    .WithComponents(new[]
                    {
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Назад")
                            .WithCustomId($"voice_top_page_{page - 1}")
                            .WithDisabled(page <= 1)
                            .WithEmote(new Emoji("⬅️")),
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Обновить")
                            .WithCustomId($"voice_top_refresh_{page}")
                            .WithDisabled(false)
                            .WithEmote(new Emoji("🔄")),
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Далее")
                            .WithCustomId($"voice_top_page_{page + 1}")
                            .WithDisabled(page >= totalPages)
                            .WithEmote(new Emoji("➡️"))
                    }))
        });
    }
}
