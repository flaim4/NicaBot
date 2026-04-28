using Discord;
using Discord.WebSocket;
using DotNetEnv;
using SubBot.Utils;
using SubBot.Database;

namespace SubBot;

internal class Program
{
    // Делаем _client публичным и статическим
    public static DiscordSocketClient? Client { get; private set; }
    private static AutoSlash? _autoSlash;

    public static async Task Main()
    {
        Logger.Init();
        Logger.Info("Загрузка переменных окружения...");
        Env.Load();

        var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            Logger.Error("Токен не найден, завершение работы.");
            return;
        }
        Logger.Info("Токен загружен, запуск клиента...");

        var discordConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildVoiceStates
        };

        Client = new DiscordSocketClient(discordConfig);
        _autoSlash = new AutoSlash(Client);

        var connectionString = ConfigLoader.GetConnectionString();

        try
        {
            using var db = new BotDbContext(connectionString);
            
            Logger.Info("Подключения к MySQL...");
            if (db.Database.CanConnect())
            {
                Logger.Info("Подключение к MySQL успешно.");

                var created = db.Database.EnsureCreated();
                if (created)
                    Logger.Info("База данных и таблицы успешно созданы.");

                await db.EnsureSchemaAsync();
            }
            else
            {
                Logger.Error("Не удалось подключиться к базе данных.");
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Ошибка при работе с базой данных: {ex.Message}");
            return;
        }

        Client.Ready += async () =>
        {
            if (_autoSlash != null) await _autoSlash.RegisterAsync();

            var guild = Client.GetGuild(1201504312841478175);
            if (guild != null)
                await guild.DownloadUsersAsync();

            await VoiceTracker.SyncActiveVoiceSessionsAsync(Client, connectionString);

            Logger.Info($"{Client.CurrentUser.Username} запущена.");
        };

        Client.SlashCommandExecuted += async cmd =>
        {
            if (_autoSlash != null)
                await _autoSlash.HandleAsync(cmd);
        };

        Client.UserVoiceStateUpdated += async (user, before, after) =>
        {
            await VoiceTracker.HandleVoiceStateUpdatedAsync(connectionString, user, before, after);
        };

        _ = new InteractionHandler(Client, connectionString);

        await Client.LoginAsync(TokenType.Bot, token);
        await Client.StartAsync();

        await Task.Delay(-1);
    }
}
