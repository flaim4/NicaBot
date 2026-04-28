namespace SubBot.Utils;

public static class VoiceTimeFormatter
{
    public static string Format(long totalSeconds)
    {
        if (totalSeconds <= 0)
            return "0 мин";

        var span = TimeSpan.FromSeconds(totalSeconds);
        var parts = new List<string>();

        if (span.Days > 0)
            parts.Add($"{span.Days} д");

        if (span.Hours > 0)
            parts.Add($"{span.Hours} ч");

        if (span.Minutes > 0)
            parts.Add($"{span.Minutes} мин");

        if (parts.Count == 0)
            parts.Add("меньше минуты");

        return string.Join(" ", parts);
    }
}
