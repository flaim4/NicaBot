using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace SubBot.Database;

public class BotDbContext : DbContext
{
    private readonly string _connectionString;

    public BotDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<User> Users { get; set; }

    public DbSet<UserBalance> UserBalances { get; set; }

    public DbSet<UserVoiceStat> UserVoiceStats { get; set; }

    public DbSet<AcceptedApplication> AcceptedApplications { get; set; }

    public DbSet<VanillaApplication> VanillaApplications { get; set; }

    public DbSet<Clan> Clans { get; set; }

    public DbSet<ClanInvite> ClanInvites { get; set; }

    public DbSet<UserMinecraftProfile> UserMinecraftProfiles { get; set; }

    public DbSet<PendingClanCreation> PendingClanCreations { get; set; }

    public async Task EnsureSchemaAsync()
    {
        var connection = Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
            await connection.OpenAsync();

        try
        {
            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `Users` (
                    `Id` BIGINT UNSIGNED NOT NULL,
                    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
                );
                """);

            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `UserBalances` (
                    `UserId` BIGINT UNSIGNED NOT NULL,
                    `Coins` BIGINT NOT NULL DEFAULT 0,
                    CONSTRAINT `PK_UserBalances` PRIMARY KEY (`UserId`),
                    CONSTRAINT `FK_UserBalances_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                );
                """);

            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `UserVoiceStats` (
                    `UserId` BIGINT UNSIGNED NOT NULL,
                    `TotalSeconds` BIGINT NOT NULL DEFAULT 0,
                    `SessionStartedAtUtc` DATETIME NULL,
                    CONSTRAINT `PK_UserVoiceStats` PRIMARY KEY (`UserId`),
                    CONSTRAINT `FK_UserVoiceStats_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                );
                """);

            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `AcceptedApplications` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `ApplicationCode` VARCHAR(32) NOT NULL,
                    `UserId` BIGINT UNSIGNED NOT NULL,
                    `ReviewedByUserId` BIGINT UNSIGNED NOT NULL,
                    `MinecraftNick` VARCHAR(64) NOT NULL,
                    `Age` VARCHAR(16) NOT NULL,
                    `Interest` VARCHAR(1000) NOT NULL,
                    `AcceptedAtUtc` DATETIME NOT NULL,
                    CONSTRAINT `PK_AcceptedApplications` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_AcceptedApplications_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_AcceptedApplications_Users_ReviewedByUserId` FOREIGN KEY (`ReviewedByUserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                );
                """);

            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `VanillaApplications` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `ApplicationCode` VARCHAR(32) NOT NULL,
                    `UserId` BIGINT UNSIGNED NOT NULL,
                    `MinecraftNick` VARCHAR(64) NOT NULL,
                    `Age` VARCHAR(16) NOT NULL,
                    `Interest` VARCHAR(1000) NOT NULL,
                    `NotificationsChannelId` BIGINT UNSIGNED NOT NULL,
                    `Status` VARCHAR(16) NOT NULL,
                    `ReviewedByUserId` BIGINT UNSIGNED NULL,
                    `CreatedAtUtc` DATETIME NOT NULL,
                    `ReviewedAtUtc` DATETIME NULL,
                    CONSTRAINT `PK_VanillaApplications` PRIMARY KEY (`Id`)
                );
                """);

            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `Clans` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `Tag` VARCHAR(4) NOT NULL,
                    `OwnerUserId` BIGINT UNSIGNED NOT NULL,
                    `RoleId` BIGINT UNSIGNED NOT NULL,
                    `ChannelId` BIGINT UNSIGNED NOT NULL,
                    `CreatedAtUtc` DATETIME NOT NULL,
                    `NextPaymentDueAtUtc` DATETIME NOT NULL,
                    CONSTRAINT `PK_Clans` PRIMARY KEY (`Id`),
                    CONSTRAINT `AK_Clans_Tag` UNIQUE (`Tag`),
                    CONSTRAINT `AK_Clans_OwnerUserId` UNIQUE (`OwnerUserId`),
                    CONSTRAINT `FK_Clans_Users_OwnerUserId` FOREIGN KEY (`OwnerUserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                );
                """);

            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `ClanInvites` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `ClanId` INT NOT NULL,
                    `InviterUserId` BIGINT UNSIGNED NOT NULL,
                    `InvitedUserId` BIGINT UNSIGNED NOT NULL,
                    `CreatedAtUtc` DATETIME NOT NULL,
                    CONSTRAINT `PK_ClanInvites` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_ClanInvites_Clans_ClanId` FOREIGN KEY (`ClanId`) REFERENCES `Clans` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_ClanInvites_Users_InviterUserId` FOREIGN KEY (`InviterUserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_ClanInvites_Users_InvitedUserId` FOREIGN KEY (`InvitedUserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `AK_ClanInvites_ClanUser` UNIQUE (`ClanId`, `InvitedUserId`)
                );
                """);

            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `UserMinecraftProfiles` (
                    `UserId` BIGINT UNSIGNED NOT NULL,
                    `MinecraftNick` VARCHAR(64) NOT NULL,
                    `UpdatedAtUtc` DATETIME NOT NULL,
                    CONSTRAINT `PK_UserMinecraftProfiles` PRIMARY KEY (`UserId`),
                    CONSTRAINT `FK_UserMinecraftProfiles_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `AK_UserMinecraftProfiles_MinecraftNick` UNIQUE (`MinecraftNick`)
                );
                """);

            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS `PendingClanCreations` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `UserId` BIGINT UNSIGNED NOT NULL,
                    `Tag` VARCHAR(4) NOT NULL,
                    `SecretKey` VARCHAR(64) NOT NULL,
                    `Cost` BIGINT NOT NULL,
                    `CreatedAtUtc` DATETIME NOT NULL,
                    `ExpiresAtUtc` DATETIME NOT NULL,
                    `Consumed` TINYINT(1) NOT NULL DEFAULT 0,
                    CONSTRAINT `PK_PendingClanCreations` PRIMARY KEY (`Id`),
                    CONSTRAINT `AK_PendingClanCreations_SecretKey` UNIQUE (`SecretKey`),
                    CONSTRAINT `FK_PendingClanCreations_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                );
                """);

            if (!await ColumnExistsAsync(connection, "Users", "ClanId"))
            {
                await Database.ExecuteSqlRawAsync("""
                    ALTER TABLE `Users`
                    ADD COLUMN `ClanId` INT NULL;
                    """);
            }

            await TryAddUsersClanForeignKeyAsync(connection);

            await MigrateLegacyUserCoinsAsync(connection);
        }
        finally
        {
            if (shouldCloseConnection)
                await connection.CloseAsync();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasOne(x => x.Balance)
            .WithOne(x => x.User)
            .HasForeignKey<UserBalance>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(x => x.VoiceStat)
            .WithOne(x => x.User)
            .HasForeignKey<UserVoiceStat>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(x => x.Clan)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ClanId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasOne(x => x.MinecraftProfile)
            .WithOne(x => x.User)
            .HasForeignKey<UserMinecraftProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AcceptedApplication>()
            .HasOne(x => x.User)
            .WithMany(x => x.AcceptedApplications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AcceptedApplication>()
            .HasOne(x => x.ReviewedByUser)
            .WithMany(x => x.ReviewedAcceptedApplications)
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AcceptedApplication>()
            .HasIndex(x => x.ApplicationCode)
            .IsUnique();

        modelBuilder.Entity<VanillaApplication>()
            .HasIndex(x => x.ApplicationCode)
            .IsUnique();

        modelBuilder.Entity<VanillaApplication>()
            .HasIndex(x => new { x.UserId, x.Status });

        modelBuilder.Entity<Clan>()
            .HasOne(x => x.OwnerUser)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Clan>()
            .HasIndex(x => x.Tag)
            .IsUnique();

        modelBuilder.Entity<Clan>()
            .HasIndex(x => x.OwnerUserId)
            .IsUnique();

        modelBuilder.Entity<ClanInvite>()
            .HasOne(x => x.Clan)
            .WithMany(x => x.Invites)
            .HasForeignKey(x => x.ClanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClanInvite>()
            .HasOne(x => x.InviterUser)
            .WithMany(x => x.SentClanInvites)
            .HasForeignKey(x => x.InviterUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClanInvite>()
            .HasOne(x => x.InvitedUser)
            .WithMany(x => x.ReceivedClanInvites)
            .HasForeignKey(x => x.InvitedUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClanInvite>()
            .HasIndex(x => new { x.ClanId, x.InvitedUserId })
            .IsUnique();

        modelBuilder.Entity<UserMinecraftProfile>()
            .HasIndex(x => x.MinecraftNick)
            .IsUnique();

        modelBuilder.Entity<PendingClanCreation>()
            .HasOne(x => x.User)
            .WithMany(x => x.PendingClanCreations)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PendingClanCreation>()
            .HasIndex(x => x.SecretKey)
            .IsUnique();
    }

    private async Task MigrateLegacyUserCoinsAsync(DbConnection connection)
    {
        if (!await TableExistsAsync(connection, "UserCoins"))
            return;

        await Database.ExecuteSqlRawAsync("""
            INSERT INTO `Users` (`Id`)
            SELECT `UserId`
            FROM `UserCoins`
            ON DUPLICATE KEY UPDATE `Id` = `Id`;
            """);

        await Database.ExecuteSqlRawAsync("""
            INSERT INTO `UserBalances` (`UserId`, `Coins`)
            SELECT `UserId`, `Coins`
            FROM `UserCoins`
            ON DUPLICATE KEY UPDATE `Coins` = VALUES(`Coins`);
            """);

        await Database.ExecuteSqlRawAsync("""
            INSERT INTO `UserVoiceStats` (`UserId`, `TotalSeconds`, `SessionStartedAtUtc`)
            SELECT `UserId`, `VoiceTimeSeconds`, `VoiceSessionStartedAtUtc`
            FROM `UserCoins`
            ON DUPLICATE KEY UPDATE
                `TotalSeconds` = VALUES(`TotalSeconds`),
                `SessionStartedAtUtc` = VALUES(`SessionStartedAtUtc`);
            """);
    }

    private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName;
            """;

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(DbConnection connection, string tableName, string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName;
            """;

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var columnParam = command.CreateParameter();
        columnParam.ParameterName = "@columnName";
        columnParam.Value = columnName;
        command.Parameters.Add(columnParam);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private async Task TryAddUsersClanForeignKeyAsync(DbConnection connection)
    {
        if (!await ColumnExistsAsync(connection, "Users", "ClanId"))
            return;

        if (!await IndexExistsAsync(connection, "Users", "IX_Users_ClanId"))
        {
            await Database.ExecuteSqlRawAsync("""
                CREATE INDEX `IX_Users_ClanId`
                ON `Users` (`ClanId`);
                """);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'Users'
              AND CONSTRAINT_NAME = 'FK_Users_Clans_ClanId';
            """;

        var result = await command.ExecuteScalarAsync();
        if (Convert.ToInt32(result) > 0)
            return;

        await Database.ExecuteSqlRawAsync("""
            ALTER TABLE `Users`
            ADD CONSTRAINT `FK_Users_Clans_ClanId`
            FOREIGN KEY (`ClanId`) REFERENCES `Clans` (`Id`) ON DELETE SET NULL;
            """);
    }

    private static async Task<bool> IndexExistsAsync(DbConnection connection, string tableName, string indexName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND INDEX_NAME = @indexName;
            """;

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var indexParam = command.CreateParameter();
        indexParam.ParameterName = "@indexName";
        indexParam.Value = indexName;
        command.Parameters.Add(indexParam);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 33));
        options.UseMySql(_connectionString, serverVersion);
    }
}
