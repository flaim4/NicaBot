using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;

public static class ConfigLoader
{
    private static readonly Lazy<BotConfig> _config = new(() =>
    {
        var yaml = File.ReadAllText("config.yml");

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<BotConfig>(yaml);
    });

    public static BotConfig Load() => _config.Value;

    public static string GetConnectionString()
    {
        var mysql = Load().Mysql;
        return $"server={mysql.Host};port={mysql.Port};database={mysql.Database};uid={mysql.Username};pwd={mysql.Password};";
    }
} 
