# NicaBot

## Как начать использовать NicaBot

### Скачать и установить .NET SDK 10

Скачать и установить .NET SDK 10 можно с официального сайта Microsoft: https://dotnet.microsoft.com/download/dotnet/10.0

### Склонировать репозиторий

Склонируйте репозиторий NicaBot с помощью Git:

```bash
git clone https://github.com/flaim4/NicaBot.git
cd NicaBot
```

### Установить зависимости

Установите все необходимые зависимости, используя команду:

```bash
dotnet restore
```

### Настроить конфигурацию

Создайте файл `.env` в корневой директории проекта и добавьте следующие переменные окружения:

```conf
DISCORD_BOT_TOKEN=your_discord_bot_token
```

Создайте файл `config.yml` в корневой директории проекта и добавьте следующие настройки:

```yaml
commands:
    авторизация: true
    профиль: false
    аватар: true
    ежедневный_бонус: true
    топ: false

mysql:
    host: ip
    port: 3306
    database: subbot
    username: root
    password:

leaderRoles:
    rubyLeaderRoleId: role_id_1
    voiceLeaderRoleId: role_id_2
```

Замените mysql данные на свои.
leaderRoles - это ID ролей, которые будут использоваться для выдачи лидерам, создайте 2 роли и их ID добавьте в конфиг.

### Собрать и запустить бота

Соберите и запустите бота с помощью команды:

```bash
dotnet run
# или
dotnet build && dotnet run
```
