using Discord;
using Discord.WebSocket;
using SubBot.Database;
using SubBot.Utils;

public static class ProfileCommand
{
    [Slash("профиль", "Посмотреть свой профиль или профиль другого пользователя")]
    [SlashOption(
        name: "пользователь",
        description: "Пользователь, профиль которого хотите посмотреть",
        type: ApplicationCommandOptionType.User,
        required: false
    )]
    public static async Task Profile(SocketSlashCommand cmd)
    {
        try
        {
            using var db = new BotDbContext(ConfigLoader.GetConnectionString());

            var targetUser = cmd.Data.Options.FirstOrDefault(x => x.Name == "пользователь")?.Value as SocketUser ?? cmd.User;
            var targetGuildUser = targetUser as SocketGuildUser;

            var user = await DbUserService.GetOrCreateFullUserAsync(db, targetUser.Id);
            await db.SaveChangesAsync();

            var profileText = $"**Профиль — {DiscordMentionFormatter.User(targetUser.Id)}**\n" +
                              "-# Информация о пользователе и его достижениях\n\n" +
                              $"<:ruby:1482366823159824545> **Рубины:** {user.Balance!.Coins}\n" +
                              $"<:microphone2:1487459623583594607> **Время в войсе:** {VoiceTimeFormatter.Format(VoiceTracker.GetCurrentVoiceSeconds(user.VoiceStat!))}\n";

            if (targetGuildUser != null)
            {
                var status = targetGuildUser.Status switch
                {
                    UserStatus.Online => "В сети",
                    UserStatus.Idle => "Отошёл",
                    UserStatus.DoNotDisturb => "Не беспокоить",
                    UserStatus.Offline => "Не в сети",
                    _ => "Не в сети"
                };

                profileText += $"<:3_:1482372335418736670> **Статус:** {status}\n";
            }

            var components = new ComponentBuilderV2(new[]
            {
                new ContainerBuilder()
                    .WithMediaGallery(
                        new MediaGalleryBuilder()
                            .AddItems(new MediaGalleryItemProperties(
                                targetUser.GetAvatarUrl() ?? targetUser.GetDefaultAvatarUrl()
                            ))
                    )
                    .WithTextDisplay(new TextDisplayBuilder()
                        .WithContent(profileText))
            });

            await cmd.RespondAsync(
                components: components.Build(),
                ephemeral: false,
                allowedMentions: AllowedMentions.None
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в Profile: {ex.Message}");
            await cmd.RespondAsync("Произошла ошибка при получении профиля.", ephemeral: true);
        }
    }
}
