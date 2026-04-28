using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SubBot.Database;
using SubBot.Utils;

public static class AuthCommands
{
    private const ulong AuthorizedRoleId = 1208444981849620642;
    private const string AuthorizationButtonId = "79b40ceffd8f480b978c40fd20b906cf";
    private const string AuthorizationModalId = "authorization_minecraft_nick_modal";
    private const string AuthorizationNickFieldId = "authorization_minecraft_nick";

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
        if (comp.Data.CustomId != AuthorizationButtonId)
            return;

        var currentNick = await db.UserMinecraftProfiles
            .Where(x => x.UserId == comp.User.Id)
            .Select(x => x.MinecraftNick)
            .FirstOrDefaultAsync();

        var modal = new ModalBuilder()
            .WithTitle("Авторизация")
            .WithCustomId(AuthorizationModalId)
            .AddTextInput(
                "Ник в игре",
                AuthorizationNickFieldId,
                TextInputStyle.Short,
                placeholder: "Например: _f1a1m_",
                required: true,
                value: currentNick,
                maxLength: 32);

        await comp.RespondWithModalAsync(modal.Build());
    }

    [ModalEvent]
    public static async Task HandleAuthorizationModal(SocketModal modal, BotDbContext db)
    {
        if (modal.Data.CustomId != AuthorizationModalId)
            return;

        if (modal.User is not SocketGuildUser guildUser)
        {
            await modal.RespondAsync("Не удалось получить данные участника сервера.", ephemeral: true);
            return;
        }

        var minecraftNick = modal.Data.Components
            .FirstOrDefault(x => x.CustomId == AuthorizationNickFieldId)?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(minecraftNick))
        {
            await modal.RespondAsync("Укажите ник в игре.", ephemeral: true);
            return;
        }

        var role = guildUser.Guild.GetRole(AuthorizedRoleId);
        if (role == null)
        {
            await modal.RespondAsync("Роль авторизации не найдена на сервере.", ephemeral: true);
            return;
        }

        try
        {
            await DbUserService.GetOrCreateFullUserAsync(db, guildUser.Id);
            await DbUserService.SetMinecraftNickAsync(db, guildUser.Id, minecraftNick);
            await db.SaveChangesAsync();

            if (!guildUser.Roles.Any(x => x.Id == role.Id))
                await guildUser.AddRoleAsync(role);

            await modal.RespondAsync(
                components: BuildStatusComponent(
                    "Авторизация успешна",
                    $"Роль выдана.\nНик в игре: {DiscordTextFormatter.EscapeMarkdown(minecraftNick)}"
                ).Build(),
                ephemeral: true,
                allowedMentions: AllowedMentions.None
            );
        }
        catch (InvalidOperationException ex)
        {
            await modal.RespondAsync(ex.Message, ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Не удалось завершить авторизацию для {guildUser.Id}: {ex.Message}");
            await modal.RespondAsync(
                components: BuildStatusComponent(
                    "Не удалось завершить авторизацию",
                    "Проверьте права бота и попробуйте ещё раз."
                ).Build(),
                ephemeral: true
            );
        }
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
