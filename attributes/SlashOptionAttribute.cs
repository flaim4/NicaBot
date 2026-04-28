using Discord;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class SlashOptionAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }
    public ApplicationCommandOptionType Type { get; }
    public bool Required { get; }

    public SlashOptionAttribute(
        string name,
        string description,
        ApplicationCommandOptionType type,
        bool required = false)
    {
        Name = name;
        Description = description;
        Type = type;
        Required = required;
    }
}
