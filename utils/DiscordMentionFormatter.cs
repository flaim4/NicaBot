namespace SubBot.Utils;

public static class DiscordMentionFormatter
{
    public static string User(ulong userId) => $"<@{userId}>";
}
