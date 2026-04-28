namespace SubBot.Utils;

public static class DiscordTextFormatter
{
    private static readonly char[] MarkdownChars =
    {
        '\\', '*', '_', '~', '`', '>', '|', '[', ']', '(', ')', '#', '+', '-', '!'
    };

    public static string EscapeMarkdown(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Не указано";

        var result = value.Trim();
        foreach (var ch in MarkdownChars)
            result = result.Replace(ch.ToString(), $"\\{ch}");

        return result;
    }

    public static string NormalizeInput(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Не указано"
            : value.Trim();
    }
}
