using Discord;
using Discord.WebSocket;
using System.Reflection;

namespace SubBot.Utils;

public class AutoSlash
{
    private readonly DiscordSocketClient _client;
    private readonly Dictionary<string, MethodInfo> _handlers = new();
    private readonly BotConfig _config;

    public AutoSlash(DiscordSocketClient client)
    {
        _client = client;
        _config = ConfigLoader.Load();
    }

    public async Task RegisterAsync()
    {
        var methods = Assembly.GetExecutingAssembly()
            .GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.GetCustomAttribute<SlashAttribute>() != null)
            .ToList();

        var enabledMethods = methods
            .Where(method =>
            {
                var slashAttr = method.GetCustomAttribute<SlashAttribute>()!;
                return !_config.Commands.TryGetValue(slashAttr.Name, out var enabled) || enabled;
            })
            .ToList();

        var total = enabledMethods.Count;
        var current = 0;

        Logger.Info($"Начинается регистрация команд ({current}/{total})...");

        foreach (var method in enabledMethods)
        {
            var slashAttr = method.GetCustomAttribute<SlashAttribute>()!;
            var name = slashAttr.Name;

            _handlers[name] = method;

            var cmd = new SlashCommandBuilder()
                .WithName(name)
                .WithDescription(slashAttr.Description);

            var optionAttributes = method.GetCustomAttributes<SlashOptionAttribute>();
            foreach (var option in optionAttributes)
            {
                cmd.AddOption(new SlashCommandOptionBuilder()
                    .WithName(option.Name)
                    .WithDescription(option.Description)
                    .WithType(option.Type)
                    .WithRequired(option.Required));
            }

            await _client.CreateGlobalApplicationCommandAsync(cmd.Build());

            current++;
            Logger.Info($"Регистрация команд ({current}/{total})...");
        }

        Logger.Info("Регистрация всех команд завершена.");
    }

    public async Task HandleAsync(SocketSlashCommand cmd)
    {
        if (!_handlers.TryGetValue(cmd.Data.Name, out var method))
            return;

        try
        {
            var task = (Task)method.Invoke(null, new object[] { cmd })!;
            await task;
        }
        catch (Exception ex)
        {
            Logger.Error($"Ошибка при выполнении команды {cmd.Data.Name}: {ex.Message}");
        }
    }
}
