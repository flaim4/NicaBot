using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SubBot.Database;
using SubBot.Utils;

public static class ClanCommand
{
    private const long ClanCreateCost = 5000;
    private const int MaxClanTagLength = 4;
    private const int ClanKeyLifetimeMinutes = 15;
    private const string ClanPanelImageUrl = "https://cdn.discordapp.com/attachments/1381952591981707355/1494011975796133988/image.png?ex=69e10e94&is=69dfbd14&hm=7a2edf8de2b803e91191cf99115ba5cccfa3c25d73b520866d6fca9cb7bcfb47&";
    private const string ClanPanelCreateButtonId = "clan_panel_create";
    private const string ClanCreateModalId = "clan_create_modal";
    private const string ClanCreateTagFieldId = "clan_create_tag";

    [Slash("клан_панель", "Отправить панель создания клана")]
    [SlashOption(
        name: "канал",
        description: "Текстовый канал",
        type: ApplicationCommandOptionType.Channel,
        required: true
    )]
    public static async Task SendClanPanel(SocketSlashCommand cmd)
    {
        if (cmd.User is SocketGuildUser guildUser && !guildUser.GuildPermissions.Administrator)
        {
            await cmd.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        if (cmd.Data.Options.FirstOrDefault(x => x.Name == "канал")?.Value is not SocketTextChannel targetChannel)
        {
            await cmd.RespondAsync("Канал не найден.", ephemeral: true);
            return;
        }

        await targetChannel.SendMessageAsync(
            components: BuildClanPanelComponents().Build(),
            allowedMentions: AllowedMentions.None);

        await cmd.RespondAsync($"Панель клана отправлена в {targetChannel.Mention}.", ephemeral: true);
    }

    [InteractionEvent]
    public static async Task HandleClanPanelButtons(SocketMessageComponent comp, BotDbContext db)
    {
        if (comp.Data.CustomId != ClanPanelCreateButtonId)
            return;

        var modal = new ModalBuilder()
            .WithTitle("Создание клана")
            .WithCustomId(ClanCreateModalId)
            .AddTextInput(
                "Название клана",
                ClanCreateTagFieldId,
                TextInputStyle.Short,
                placeholder: "Например: WOLF",
                required: true,
                maxLength: MaxClanTagLength);

        await comp.RespondWithModalAsync(modal.Build());
    }

    [ModalEvent]
    public static async Task HandleClanCreateModal(SocketModal modal, BotDbContext db)
    {
        if (modal.Data.CustomId != ClanCreateModalId)
            return;

        var guild = (modal.Channel as SocketGuildChannel)?.Guild;
        if (guild == null)
        {
            await modal.RespondAsync("Клан можно создать только на сервере.", ephemeral: true);
            return;
        }

        var rawTag = modal.Data.Components.FirstOrDefault(x => x.CustomId == ClanCreateTagFieldId)?.Value;
        var result = await CreatePendingClanAsync(db, modal.User.Id, rawTag);

        if (!result.Success)
        {
            await modal.RespondAsync(
                components: BuildClanKeyComponent("Ошибка создания клана", result.Message).Build(),
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        var commandText = $"/clan create {result.SecretKey}";
        var dmStatus = "Команда также отправлена вам в ЛС.";

        try
        {
            var dm = await modal.User.CreateDMChannelAsync();
            await dm.SendMessageAsync(
                components: BuildClanKeyComponent(
                    $"Создание клана {result.Tag}",
                    $"Введите эту команду в чате Minecraft в течение **{ClanKeyLifetimeMinutes} минут**:\n`{commandText}`\n" +
                    $"-# После успешного создания спишется **{ClanCreateCost}** <:ruby:1482366823159824545>\n" +
                    "-# Никому не показывайте этот ключ."
                ).Build(),
                allowedMentions: AllowedMentions.None
            );
        }
        catch
        {
            dmStatus = "Не удалось отправить команду в ЛС, поэтому используйте команду из этого скрытого сообщения.";
        }

        await modal.RespondAsync(
            components: BuildClanKeyComponent(
                $"Клан {result.Tag} подготовлен",
                $"Введите в Minecraft в течение **{ClanKeyLifetimeMinutes} минут**:\n`{commandText}`\n" +
                $"После успешного создания спишется **{ClanCreateCost}** <:ruby:1482366823159824545>\n" +
                dmStatus
            ).Build(),
            ephemeral: true,
            allowedMentions: AllowedMentions.None
        );
    }

    private static ComponentBuilderV2 BuildClanPanelComponents()
    {
        return new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithMediaGallery(
                    new MediaGalleryBuilder()
                        .AddItems(new MediaGalleryItemProperties(ClanPanelImageUrl))
                )
                .WithTextDisplay(
                    new TextDisplayBuilder()
                        .WithContent(
                            "**Создание клана**\n" +
                            "Создай свой тег и получи ключ для подтверждения в Minecraft.\n" +
                            $"-# Стоимость создания: **{ClanCreateCost}** <:ruby:1482366823159824545>\n" +
                            $"-# Ключ действует **{ClanKeyLifetimeMinutes} минут**.\n" +
                            "-# Рубины спишутся только после успешного создания клана в игре.\n" +
                            "-# Пока один ключ активен, новый создать нельзя."
                        )
                )
                .WithActionRow(
                    new ActionRowBuilder()
                        .WithComponents(new[]
                        {
                            new ButtonBuilder()
                                .WithStyle(ButtonStyle.Secondary)
                                .WithLabel("Создать клан")
                                .WithCustomId(ClanPanelCreateButtonId)
                                .WithEmote(Emote.Parse("<:ruby:1482366823159824545>")),
                            new ButtonBuilder()
                                .WithStyle(ButtonStyle.Link)
                                .WithLabel("Поддержка")
                                .WithUrl("https://github.com/flaim4")
                        })
                )
        });
    }

    private static ComponentBuilderV2 BuildClanKeyComponent(string title, string description)
    {
        return new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithMediaGallery(
                    new MediaGalleryBuilder()
                        .AddItems(new MediaGalleryItemProperties(ClanPanelImageUrl))
                )
                .WithTextDisplay(
                    new TextDisplayBuilder()
                        .WithContent(
                            $"**{title}**\n" +
                            "-# Всё в стиле ежедневного бонуса.\n\n" +
                            $"{description}"
                        )
                )
        });
    }

    private static async Task<(bool Success, string Message, string Tag, string SecretKey)> CreatePendingClanAsync(
        BotDbContext db,
        ulong userId,
        string? rawTag)
    {
        var normalizedTag = NormalizeClanTag(rawTag);
        if (normalizedTag == null)
            return (false, "Название клана должно содержать только буквы и быть длиной от 1 до 4 символов.", "", "");

        var user = await DbUserService.GetOrCreateFullUserAsync(db, userId);
        await db.SaveChangesAsync();

        if (user.ClanId.HasValue)
            return (false, "Вы уже состоите в клане.", "", "");

        if (user.Balance!.Coins < ClanCreateCost)
            return (false, $"Для создания клана нужно **{ClanCreateCost}** рубинов. Сейчас у вас **{user.Balance.Coins}**.", "", "");

        if (await db.Clans.AnyAsync(x => x.Tag == normalizedTag))
            return (false, "Клан с таким тегом уже существует.", "", "");

        var activePending = await db.PendingClanCreations
            .Where(x => x.UserId == userId && !x.Consumed && x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();
        if (activePending != null)
        {
            return (false,
                $"У вас уже есть активный ключ. Он действует {ClanKeyLifetimeMinutes} минут.\n`/clan create {activePending.SecretKey}`",
                "",
                "");
        }

        var secretKey = $"clan-{Guid.NewGuid():N}"[..20];
        db.PendingClanCreations.Add(new PendingClanCreation
        {
            UserId = userId,
            Tag = normalizedTag,
            SecretKey = secretKey,
            Cost = ClanCreateCost,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(ClanKeyLifetimeMinutes),
            Consumed = false
        });

        await db.SaveChangesAsync();
        return (true, "", normalizedTag, secretKey);
    }

    private static string? NormalizeClanTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length is < 1 or > MaxClanTagLength)
            return null;

        return normalized.All(char.IsLetter) ? normalized : null;
    }
}
