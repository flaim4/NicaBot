using Discord.WebSocket;
using SubBot.Database;
using System.Reflection;

namespace SubBot.Utils
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly string _connectionString;
        private readonly List<MethodInfo> _componentMethods = new();
        private readonly List<MethodInfo> _modalMethods = new();

        public InteractionHandler(DiscordSocketClient client, string connectionString)
        {
            _client = client;
            _connectionString = connectionString;

            Register();
            _client.InteractionCreated += OnInteraction;
        }

        private void Register()
        {
            var methods = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .ToList();

            _componentMethods.AddRange(methods.Where(m =>
                m.GetCustomAttribute<InteractionEventAttribute>() != null &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == typeof(SocketMessageComponent) &&
                m.GetParameters()[1].ParameterType == typeof(BotDbContext)));

            _modalMethods.AddRange(methods.Where(m =>
                m.GetCustomAttribute<ModalEventAttribute>() != null &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == typeof(SocketModal) &&
                m.GetParameters()[1].ParameterType == typeof(BotDbContext)));
        }

        private async Task OnInteraction(SocketInteraction interaction)
        {
            using var db = new BotDbContext(_connectionString);

            if (interaction is SocketMessageComponent comp)
            {
                foreach (var method in _componentMethods)
                {
                    try
                    {
                        await (Task)method.Invoke(null, new object[] { comp, db })!;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Ошибка component interaction: {ex.Message}");
                    }
                }

                return;
            }

            if (interaction is not SocketModal modal)
                return;

            foreach (var method in _modalMethods)
            {
                try
                {
                    await (Task)method.Invoke(null, new object[] { modal, db })!;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка modal interaction: {ex.Message}");
                }
            }
        }
    }
}
