using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SubBot.Database;
using SubBot.Utils;

public static class VanillaApplyCommand
{
    private const string VanillaApplyImageUrl = "https://cdn.discordapp.com/attachments/1381952591981707355/1487609920206409859/Frame_37.png?ex=69c9c433&is=69c872b3&hm=ae479b097d5ac1d93762f1096a2c4ee0ffd3e6909782fee09a76ad9bbd7c02d0&";
    private const string OpenApplyPrefix = "vanilla_apply_open:";
    private const string ModalPrefix = "vanilla_apply_modal:";
    private const string AcceptPrefix = "vanilla_apply_accept:";
    private const string RejectPrefix = "vanilla_apply_reject:";

    [Slash("анкета_ванилла", "Отправить панель анкеты на ванильный Minecraft проект")]
    [SlashOption(
        name: "канал",
        description: "Канал, куда отправить панель с кнопкой",
        type: ApplicationCommandOptionType.Channel,
        required: true
    )]
    [SlashOption(
        name: "канал_анкет",
        description: "Канал, куда будут приходить заполненные анкеты",
        type: ApplicationCommandOptionType.Channel,
        required: true
    )]
    [SlashOption(
        name: "канал_уведомлений",
        description: "Канал, куда будут приходить уведомления о принятии или отказе",
        type: ApplicationCommandOptionType.Channel,
        required: true
    )]
    public static async Task SendVanillaApplyPanel(SocketSlashCommand cmd)
    {
        if (cmd.User is SocketGuildUser guildUser && !guildUser.GuildPermissions.Administrator)
        {
            await cmd.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        var panelChannel = cmd.Data.Options.FirstOrDefault(x => x.Name == "канал")?.Value as SocketTextChannel;
        var applicationsChannel = cmd.Data.Options.FirstOrDefault(x => x.Name == "канал_анкет")?.Value as SocketTextChannel;
        var notificationsChannel = cmd.Data.Options.FirstOrDefault(x => x.Name == "канал_уведомлений")?.Value as SocketTextChannel;

        if (panelChannel == null || applicationsChannel == null || notificationsChannel == null)
        {
            await cmd.RespondAsync("Один из указанных каналов не найден или не является текстовым.", ephemeral: true);
            return;
        }

        var customId = $"{OpenApplyPrefix}{applicationsChannel.Id}:{notificationsChannel.Id}";
        var components = new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithMediaGallery(
                    new MediaGalleryBuilder()
                        .AddItems(new MediaGalleryItemProperties(VanillaApplyImageUrl))
                )
                .WithTextDisplay(new TextDisplayBuilder()
                    .WithContent(
                        "**Анкета на ванильный Minecraft проект**\n" +
                        "-# Хочешь попасть к нам на проект? Заполни короткую анкету, и мы её рассмотрим.\n\n" +
                        "Нажми кнопку ниже, чтобы открыть форму.\n" +
                        "-# В анкете мы спросим ник, возраст и что именно тебя заинтересовало."
                    ))
                .WithActionRow(new ActionRowBuilder()
                    .WithComponents(new[]
                    {
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Подать анкету")
                            .WithCustomId(customId),
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Link)
                            .WithLabel("Поддержка")
                            .WithUrl("https://github.com/flaim4")
                    }))
        });

        await panelChannel.SendMessageAsync(
            components: components.Build(),
            allowedMentions: AllowedMentions.None
        );

        await cmd.RespondAsync($"Панель анкеты отправлена в {panelChannel.Mention}.", ephemeral: true);
    }

    [InteractionEvent]
    public static async Task HandleOpenApplyButton(SocketMessageComponent comp, BotDbContext db)
    {
        if (!comp.Data.CustomId.StartsWith(OpenApplyPrefix))
            return;

        var payload = comp.Data.CustomId[OpenApplyPrefix.Length..];
        var parts = payload.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            await comp.RespondAsync("Не удалось открыть анкету.", ephemeral: true);
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle("Анкета на ванильный проект")
            .WithCustomId($"{ModalPrefix}{parts[0]}:{parts[1]}")
            .AddTextInput("Ник в игре", "minecraft_nick", TextInputStyle.Short, placeholder: "Например: easy_name", required: true, maxLength: 32)
            .AddTextInput("Возраст", "age", TextInputStyle.Short, placeholder: "Например: 19", required: true, maxLength: 3)
            .AddTextInput("Что вас заинтересовало?", "interest", TextInputStyle.Paragraph, placeholder: "Расскажите, почему хотите играть именно у нас", required: true, maxLength: 500);

        await comp.RespondWithModalAsync(modal.Build());
    }

    [ModalEvent]
    public static async Task HandleApplyModal(SocketModal modal, BotDbContext db)
    {
        if (!modal.Data.CustomId.StartsWith(ModalPrefix))
            return;

        var payload = modal.Data.CustomId[ModalPrefix.Length..];
        var parts = payload.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 ||
            !ulong.TryParse(parts[0], out var applicationsChannelId) ||
            !ulong.TryParse(parts[1], out var notificationsChannelId))
        {
            await modal.RespondAsync("Не удалось обработать анкету.", ephemeral: true);
            return;
        }

        var guild = (modal.Channel as SocketGuildChannel)?.Guild;
        if (guild == null)
        {
            await modal.RespondAsync("Анкету можно подать только на сервере.", ephemeral: true);
            return;
        }

        var applicationsChannel = guild.GetTextChannel(applicationsChannelId);
        if (applicationsChannel == null)
        {
            await modal.RespondAsync("Канал для анкет не найден.", ephemeral: true);
            return;
        }

        var hasPendingApplication = await db.VanillaApplications
            .AnyAsync(x => x.UserId == modal.User.Id && x.Status == "pending");
        if (hasPendingApplication)
        {
            await modal.RespondAsync("У вас уже есть активная заявка на рассмотрении.", ephemeral: true);
            return;
        }

        var alreadyAccepted = await db.AcceptedApplications.AnyAsync(x => x.UserId == modal.User.Id);
        if (alreadyAccepted)
        {
            await modal.RespondAsync("Вы уже приняты по анкете и не можете подать её повторно.", ephemeral: true);
            return;
        }

        var fields = modal.Data.Components.ToDictionary(x => x.CustomId, x => x.Value?.Trim() ?? string.Empty);
        var minecraftNick = GetFieldValue(fields, "minecraft_nick");
        var age = GetFieldValue(fields, "age");
        var interest = GetFieldValue(fields, "interest");
        var applicationId = await GenerateUniqueApplicationCodeAsync(db);

        db.VanillaApplications.Add(new VanillaApplication
        {
            ApplicationCode = applicationId,
            UserId = modal.User.Id,
            MinecraftNick = minecraftNick,
            Age = age,
            Interest = interest,
            NotificationsChannelId = notificationsChannelId,
            Status = "pending",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var components = BuildApplicationComponents(applicationId, modal.User.Id, notificationsChannelId, minecraftNick, age, interest);

        await applicationsChannel.SendMessageAsync(
            components: components.Build(),
            allowedMentions: AllowedMentions.None
        );

        await modal.RespondAsync(
            "Анкета отправлена. Когда решение будет принято, в указанный канал придёт уведомление.",
            ephemeral: true
        );
    }

    [InteractionEvent]
    public static async Task HandleApplicationDecision(SocketMessageComponent comp, BotDbContext db)
    {
        var isAccept = comp.Data.CustomId.StartsWith(AcceptPrefix);
        var isReject = comp.Data.CustomId.StartsWith(RejectPrefix);
        if (!isAccept && !isReject)
            return;

        if (comp.User is SocketGuildUser guildUser && !guildUser.GuildPermissions.Administrator)
        {
            await comp.RespondAsync("У вас нет прав администратора для модерации анкет.", ephemeral: true);
            return;
        }

        var prefix = isAccept ? AcceptPrefix : RejectPrefix;
        var payload = comp.Data.CustomId[prefix.Length..];
        var parts = payload.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !ulong.TryParse(parts[1], out _) || !ulong.TryParse(parts[2], out _))
        {
            await comp.RespondAsync("Не удалось обработать решение по анкете.", ephemeral: true);
            return;
        }

        var applicationId = parts[0];
        var application = await db.VanillaApplications
            .FirstOrDefaultAsync(x => x.ApplicationCode == applicationId);
        if (application == null)
        {
            await comp.RespondAsync("Заявка не найдена в базе данных. Возможно, она была создана до обновления бота.", ephemeral: true);
            return;
        }

        if (application.Status != "pending")
        {
            await comp.RespondAsync("Эта анкета уже была рассмотрена.", ephemeral: true);
            return;
        }

        application.Status = isAccept ? "accepted" : "rejected";
        application.ReviewedByUserId = comp.User.Id;
        application.ReviewedAtUtc = DateTime.UtcNow;

        var guild = (comp.Channel as SocketGuildChannel)?.Guild;
        var notificationsChannel = guild?.GetTextChannel(application.NotificationsChannelId);

        var decisionText = isAccept ? "принята" : "отклонена";
        var decisionTitle = isAccept ? "Анкета принята" : "Анкета отклонена";
        var reviewerMention = DiscordMentionFormatter.User(comp.User.Id);
        var applicantMention = DiscordMentionFormatter.User(application.UserId);

        if (isAccept)
        {
            await DbUserService.GetOrCreateFullUserAsync(db, application.UserId);
            await DbUserService.GetOrCreateFullUserAsync(db, comp.User.Id);
            await DbUserService.SetMinecraftNickAsync(db, application.UserId, application.MinecraftNick);

            var alreadySaved = await db.AcceptedApplications.AnyAsync(x => x.ApplicationCode == applicationId);
            if (!alreadySaved)
            {
                db.AcceptedApplications.Add(new AcceptedApplication
                {
                    ApplicationCode = applicationId,
                    UserId = application.UserId,
                    ReviewedByUserId = comp.User.Id,
                    MinecraftNick = application.MinecraftNick,
                    Age = application.Age,
                    Interest = application.Interest,
                    AcceptedAtUtc = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();

        if (notificationsChannel != null)
        {
            var notificationComponents = new ComponentBuilderV2(new[]
            {
                new ContainerBuilder()
                    .WithMediaGallery(
                        new MediaGalleryBuilder()
                            .AddItems(new MediaGalleryItemProperties(VanillaApplyImageUrl))
                    )
                    .WithTextDisplay(new TextDisplayBuilder()
                        .WithContent(
                            $"**{decisionTitle}**\n" +
                            $"-# Анкета #{applicationId} была рассмотрена.\n\n" +
                            $"Пользователь: {applicantMention}\n" +
                            $"Решение: {decisionText}\n" +
                            $"Модератор: {reviewerMention}"
                        ))
            });

            await notificationsChannel.SendMessageAsync(
                components: notificationComponents.Build(),
                allowedMentions: AllowedMentions.None
            );
        }

        var reviewedComponents = BuildReviewedApplicationComponents(
            applicationId,
            application.UserId,
            application.MinecraftNick,
            application.Age,
            application.Interest,
            decisionTitle,
            decisionText,
            reviewerMention);

        await comp.UpdateAsync(msg =>
        {
            msg.Components = reviewedComponents.Build();
            msg.AllowedMentions = AllowedMentions.None;
        });
    }

    private static ComponentBuilderV2 BuildApplicationComponents(
        string applicationId,
        ulong userId,
        ulong notificationsChannelId,
        string minecraftNick,
        string age,
        string interest)
    {
        return new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithTextDisplay(new TextDisplayBuilder()
                    .WithContent(
                        $"**Новая анкета #{applicationId}**\n" +
                        $"-# Кандидат: {DiscordMentionFormatter.User(userId)}\n\n" +
                        $"Ник в игре: {DiscordTextFormatter.EscapeMarkdown(minecraftNick)}\n" +
                        $"Возраст: {DiscordTextFormatter.EscapeMarkdown(age)}\n" +
                        $"Что заинтересовало:\n{DiscordTextFormatter.EscapeMarkdown(interest)}"
                    ))
                .WithActionRow(new ActionRowBuilder()
                    .WithComponents(new[]
                    {
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Принять")
                            .WithCustomId($"{AcceptPrefix}{applicationId}:{userId}:{notificationsChannelId}"),
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Secondary)
                            .WithLabel("Отклонить")
                            .WithCustomId($"{RejectPrefix}{applicationId}:{userId}:{notificationsChannelId}")
                    }))
        });
    }

    private static ComponentBuilderV2 BuildReviewedApplicationComponents(
        string applicationId,
        ulong userId,
        string minecraftNick,
        string age,
        string interest,
        string decisionTitle,
        string decisionText,
        string reviewerMention)
    {
        return new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithMediaGallery(
                    new MediaGalleryBuilder()
                        .AddItems(new MediaGalleryItemProperties(VanillaApplyImageUrl))
                )
                .WithTextDisplay(new TextDisplayBuilder()
                    .WithContent(
                        $"**Новая анкета #{applicationId}**\n" +
                        $"-# Кандидат: {DiscordMentionFormatter.User(userId)}\n\n" +
                        $"Ник в игре: {DiscordTextFormatter.EscapeMarkdown(minecraftNick)}\n" +
                        $"Возраст: {DiscordTextFormatter.EscapeMarkdown(age)}\n" +
                        $"Что заинтересовало:\n{DiscordTextFormatter.EscapeMarkdown(interest)}\n\n" +
                        $"Статус: {decisionTitle}\n" +
                        $"Итог: {decisionText}\n" +
                        $"Рассмотрел: {reviewerMention}"
                    ))
        });
    }

    private static string GetFieldValue(Dictionary<string, string> fields, string key)
        => fields.TryGetValue(key, out var value)
            ? DiscordTextFormatter.NormalizeInput(value)
            : "Не указано";

    private static async Task<string> GenerateUniqueApplicationCodeAsync(BotDbContext db)
    {
        while (true)
        {
            var code = Guid.NewGuid().ToString("N")[..8];
            var exists = await db.VanillaApplications.AnyAsync(x => x.ApplicationCode == code);
            if (!exists)
                return code;
        }
    }
}
