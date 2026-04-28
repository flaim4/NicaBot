[AttributeUsage(AttributeTargets.Method)]
public class SlashAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }

    public SlashAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
