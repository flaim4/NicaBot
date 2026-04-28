using Discord;
using Discord.WebSocket;

public static class RulesCommand
{

    private const string VanillaApplyImageUrl = "https://cdn.discordapp.com/attachments/1381952591981707355/1487609920206409859/Frame_37.png?ex=69c9c433&is=69c872b3&hm=ae479b097d5ac1d93762f1096a2c4ee0ffd3e6909782fee09a76ad9bbd7c02d0&";

    [Slash("правила", "Отправить панель правил сервера")]
    [SlashOption(
        name: "канал",
        description: "Текстовый канал",
        type: ApplicationCommandOptionType.Channel,
        required: true
    )]
    public static async Task Rules(SocketSlashCommand cmd)
    {
        if (cmd.User is SocketGuildUser guildUser && !guildUser.GuildPermissions.Administrator)
        {
            await cmd.RespondAsync("У вас нет прав администратора.", ephemeral: true);
            return;
        }

        var channelOption = cmd.Data.Options.FirstOrDefault(x => x.Name == "канал");
        if (channelOption?.Value is not SocketTextChannel targetChannel)
        {
            await cmd.RespondAsync("Указанный канал не найден или не является текстовым.", ephemeral: true);
            return;
        }

        var content =
            "**Правила сервера**\n" +
            "-# Пожалуйста, ознакомьтесь перед общением. Незнание правил не освобождает от ответственности.\n\n" +

            "**Общие правила**\n" +
            "• **1.1** Уважайте участников. Запрещены оскорбления, угрозы и дискриминация.\n" +
            "-# Наказание: Мьют до 8 ч.\n\n" +
            "• **1.2** Соблюдайте приличное поведение, избегайте провокаций.\n" +
            "-# Религиозные и политические темы ведите в подходящих каналах.\n" +
            "-# Наказание: Мьют + предупреждение до 12 ч.\n\n" +
            "• **1.3** Запрещены деструктивные действия против сервера (краш, рейд и т.п.).\n" +
            "-# Наказание: Бан навсегда.\n\n" +
            "• **1.4** Запрещён деанон и слив персональных данных без согласия.\n" +
            "-# Наказание: Бан (срок по решению модерации: от 2 ч до 5 д).\n\n" +
            "• **1.5** Запрещено распространение NSFW и шок-контента.\n" +
            "-# Наказание: Бан (срок по решению модерации).\n\n" +
            "• **1.6** Запрещено полное копирование аватарки/ника участника без согласия.\n" +
            "-# Наказание: Варн до 2 ч.\n\n" +
            "• **1.7** Запрещена реклама без согласования (серверы, продукты, услуги).\n" +
            "-# Наказание: Мьют + предупреждение до 24 ч.\n\n" +

            "**Правила войса**\n" +
            "• **2.1** Запрещено злоупотребление SoundBoard/SoundPad.\n" +
            "-# Наказание: Мут/Варн (по ситуации).\n\n" +
            "• **2.2** Запрещён микрофон с сильными помехами (шипение, перегруз, bassboost).\n" +
            "-# Наказание: Варн до 2 ч.\n\n" +
            "• **2.3** Запрещено нарушать эти правила даже в приватных комнатах.\n" +
            "-# Наказание: Варн/Мут до 1 д.\n\n" +

            "**Правила чатов**\n" +
            "• **3.1** Соблюдайте тематику каналов.\n" +
            "-# Наказание: Варн/Мут до 8 ч.\n\n" +
            "• **3.2** Не оффтопьте и не уводите обсуждение от темы канала.\n" +
            "-# Наказание: Варн/Мут до 2 ч.\n\n" +
            "• **3.3** Запрещено злоупотребление BetterDiscord, плагинами и т.п.\n" +
            "-# Наказание: Варн/Бан (по ситуации).\n\n" +
            "• **3.4** Запрещён нелинкабельный ник.\n" +
            "-# Наказание: Варн (по решению модерации).\n\n" +
            "• **3.5** Запрещены спам и флуд (повторы, капс, спам символами/эмодзи).\n" +
            "-# Наказание: Мьют до 3 ч.\n\n" +
            "-# Администрация оставляет за собой право выбирать меру наказания в зависимости от тяжести нарушения и истории участника.";

        var components = new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithMediaGallery(
                    new MediaGalleryBuilder()
                        .AddItems(new MediaGalleryItemProperties(VanillaApplyImageUrl))
                )
                .WithTextDisplay(new TextDisplayBuilder().WithContent(content))
                .WithActionRow(new ActionRowBuilder()
                    .WithComponents(new[]
                    {
                        new ButtonBuilder()
                            .WithStyle(ButtonStyle.Link)
                            .WithLabel("Поддержка")
                            .WithUrl("https://github.com/flaim4")
                    }))
        });

        await targetChannel.SendMessageAsync(
            components: components.Build(),
            allowedMentions: AllowedMentions.None
        );

        await cmd.RespondAsync($"Панель правил отправлена в {targetChannel.Mention}.", ephemeral: true);
    }
}
