using Discord;
using Discord.WebSocket;
using SubBot.Database;

public static class AuthCommands
{
    private const ulong AuthorizedRoleId = 1208444981849620642;
    private const string AuthorizationButtonId = "79b40ceffd8f480b978c40fd20b906cf";

    [Slash("авторизация", "Отправить сообщение авторизации")]
    [SlashOption(
        name: "канал",
        description: "Текстовый канал",
        type: ApplicationCommandOptionType.Channel,
        required: true
    )]
    public static async Task Authorization(SocketSlashCommand cmd)
    {
        if (cmd.User is SocketGuildUser guildUser && !guildUser.GuildPermissions.Administrator)
            return;

        var channelOption = cmd.Data.Options.FirstOrDefault(x => x.Name == "канал");
        if (channelOption?.Value is not SocketTextChannel targetChannel)
        {
            await cmd.RespondAsync("Указанный канал не найден или не является текстовым.", ephemeral: true);
            return;
        }

        try
        {
            var components = new ComponentBuilderV2(new[]
            {
                new ContainerBuilder()
                    .WithMediaGallery(
                        new MediaGalleryBuilder()
                            .AddItems(new MediaGalleryItemProperties(
                                "https://cdn.discordapp.com/attachments/1364676250165182574/1488962232921493514/sefsefe56456.png?ex=69ceafa3&is=69cd5e23&hm=55bcd674a2d40fe764bbf9c1f5a8fb052e2ac490c8e523721a3c7b11a06807b2&"
                            ))
                    )
                    .WithTextDisplay(
                        new TextDisplayBuilder().WithContent(
                            "**Авторизация**\n" +
                            "-# Нажми кнопку ниже, укажи ник в игре и получи доступ к чатам сервера.\n\n" +
                            "После подтверждения тебе автоматически выдастся роль участника.\n" +
                            "-# Ник в игре нужен для связки Discord и Minecraft."
                        )
                    )
                    .WithActionRow(
                        new ActionRowBuilder()
                            .WithComponents(new[]
                            {
                                new ButtonBuilder()
                                    .WithStyle(ButtonStyle.Secondary)
                                    .WithLabel("Продолжить")
                                    .WithCustomId(AuthorizationButtonId),
                                new ButtonBuilder()
                                    .WithStyle(ButtonStyle.Link)
                                    .WithLabel("Поддержка")
                                    .WithUrl("https://github.com/flaim4")
                            })
                    )
            });

            await targetChannel.SendMessageAsync(components: components.Build(), allowedMentions: AllowedMentions.None);
            await cmd.RespondAsync($"Панель авторизации отправлена в {targetChannel.Mention}.", ephemeral: true);
        }
        catch (Exception ex)
        {
            await cmd.RespondAsync($"Произошла ошибка: {ex.Message}", ephemeral: true);
        }
    }

    [InteractionEvent]
    public static async Task HandleAuthorizationButton(SocketMessageComponent comp, BotDbContext db)
    {
        if (comp.Data.CustomId != AuthorizationButtonId) return;

        if (comp.User is not SocketGuildUser guildUser) return;

        await guildUser.AddRoleAsync(guildUser.Guild.GetRole(AuthorizedRoleId));
        
    }

    private static ComponentBuilderV2 BuildStatusComponent(string title, string description)
    {
        return new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithTextDisplay(new TextDisplayBuilder()
                .WithContent($"**{title}**\n-# {description}"))
        });
    }
}
