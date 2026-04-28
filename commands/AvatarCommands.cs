using Discord;
using Discord.WebSocket;
using SubBot.Utils;

public static class AvatarCommands
{
    [Slash("аватар", "Показать аватар пользователя")]
    [SlashOption(
        name: "пользователь",
        description: "Пользователь, чей аватар нужно показать",
        type: ApplicationCommandOptionType.User,
        required: false
    )]
    public static async Task Avatar(SocketSlashCommand cmd)
    {
        var userOption = cmd.Data.Options.FirstOrDefault(x => x.Name == "пользователь");
        var targetUser = userOption?.Value as SocketUser ?? cmd.User;

        var avatarUrl = targetUser.GetAvatarUrl(ImageFormat.Png, 512)
            ?? targetUser.GetDefaultAvatarUrl();

        var components = new ComponentBuilderV2(new[]
        {
            new ContainerBuilder()
                .WithMediaGallery(
                    new MediaGalleryBuilder()
                        .AddItems(new MediaGalleryItemProperties(avatarUrl))
                )
                .WithTextDisplay(
                    new TextDisplayBuilder()
                        .WithContent($"-# Аватар пользователя {DiscordMentionFormatter.User(targetUser.Id)}")
                )
        });

        await cmd.RespondAsync(
            components: components.Build(),
            ephemeral: false,
            allowedMentions: AllowedMentions.None
        );
    }
}
