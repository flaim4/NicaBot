public class BotConfig
{
    public Dictionary<string, bool> Commands { get; set; } = new();
    public MySqlConfig Mysql { get; set; } = new();
    public LeaderRolesConfig LeaderRoles { get; set; } = new();
}

public class MySqlConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LeaderRolesConfig
{
    public ulong RubyLeaderRoleId { get; set; } = 1488934449407070299;
    public ulong VoiceLeaderRoleId { get; set; } = 1488933935600767016;
}
