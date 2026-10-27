using System.Data;
using Finis.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(FinisDbContext context)
    {
        // 1. Ensure tables and columns exist in MySQL (schema synchronization)
        await EnsureSchemaUpToDateAsync(context);

        // 2. Check if default categories exist
        if (!await context.Categories.AnyAsync(c => c.IsDefault))
        {
            var defaultCategories = new List<Category>
            {
                // Entradas
                new() { Name = "Salário", Type = TransactionType.Entrada, ColorHex = "#10B981", Icon = "wallet", IsDefault = true },
                new() { Name = "Investimentos & Rendimentos", Type = TransactionType.Entrada, ColorHex = "#059669", Icon = "trending-up", IsDefault = true },
                new() { Name = "Freelance & Bônus", Type = TransactionType.Entrada, ColorHex = "#34D399", Icon = "award", IsDefault = true },
                new() { Name = "Outras Entradas", Type = TransactionType.Entrada, ColorHex = "#6EE7B7", Icon = "plus-circle", IsDefault = true },

                // Saídas Fixas / Contas
                new() { Name = "Aluguel & Moradia", Type = TransactionType.SaidaFixa, ColorHex = "#EF4444", Icon = "home", IsDefault = true },
                new() { Name = "Condomínio", Type = TransactionType.SaidaFixa, ColorHex = "#DC2626", Icon = "building", IsDefault = true },
                new() { Name = "Energia Elétrica", Type = TransactionType.SaidaFixa, ColorHex = "#F59E0B", Icon = "zap", IsDefault = true },
                new() { Name = "Água & Saneamento", Type = TransactionType.SaidaFixa, ColorHex = "#3B82F6", Icon = "droplet", IsDefault = true },
                new() { Name = "Internet & Celular", Type = TransactionType.SaidaFixa, ColorHex = "#8B5CF6", Icon = "wifi", IsDefault = true },
                new() { Name = "Assinaturas & Serviços", Type = TransactionType.SaidaFixa, ColorHex = "#EC4899", Icon = "tv", IsDefault = true },
                new() { Name = "Financiamento / Empréstimo", Type = TransactionType.SaidaFixa, ColorHex = "#B91C1C", Icon = "file-text", IsDefault = true },
                new() { Name = "Fatura de Cartão", Type = TransactionType.SaidaFixa, ColorHex = "#8A05BE", Icon = "credit-card", IsDefault = true },

                // Gastos Diários
                new() { Name = "Supermercado", Type = TransactionType.Diario, ColorHex = "#F97316", Icon = "shopping-cart", IsDefault = true },
                new() { Name = "Restaurante & Delivery", Type = TransactionType.Diario, ColorHex = "#FB923C", Icon = "coffee", IsDefault = true },
                new() { Name = "Combustível & Transporte", Type = TransactionType.Diario, ColorHex = "#6366F1", Icon = "truck", IsDefault = true },
                new() { Name = "Farmácia & Saúde", Type = TransactionType.Diario, ColorHex = "#14B8A6", Icon = "heart", IsDefault = true },
                new() { Name = "Lazer & Passeios", Type = TransactionType.Diario, ColorHex = "#A855F7", Icon = "smile", IsDefault = true },
                new() { Name = "Compras & Vestuário", Type = TransactionType.Diario, ColorHex = "#F43F5E", Icon = "shopping-bag", IsDefault = true },
                new() { Name = "Despesas Diversas", Type = TransactionType.Diario, ColorHex = "#6B7280", Icon = "more-horizontal", IsDefault = true }
            };

            await context.Categories.AddRangeAsync(defaultCategories);
            await context.SaveChangesAsync();
        }
        else
        {
            // Ensure "Fatura de Cartão" exists even if categories were previously seeded
            var faturaCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Fatura de Cartão");
            if (faturaCat == null)
            {
                context.Categories.Add(new Category
                {
                    Name = "Fatura de Cartão",
                    Type = TransactionType.SaidaFixa,
                    ColorHex = "#8A05BE",
                    Icon = "credit-card",
                    IsDefault = true
                });
                await context.SaveChangesAsync();
            }
        }
    }

    public static async Task EnsureSchemaUpToDateAsync(FinisDbContext context)
    {
        var conn = context.Database.GetDbConnection();
        bool wasOpen = conn.State == ConnectionState.Open;
        if (!wasOpen)
        {
            await conn.OpenAsync();
        }

        try
        {
            // 1. Create CreditCards table if it does not exist
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `CreditCards` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `UserId` int NOT NULL,
                        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
                        `Limit` decimal(18,2) NOT NULL,
                        `ClosingDay` int NOT NULL,
                        `DueDay` int NOT NULL,
                        `ColorHex` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
                        `Brand` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
                        `IsActive` tinyint(1) NOT NULL DEFAULT 1,
                        `CreatedAt` datetime(6) NOT NULL,
                        CONSTRAINT `PK_CreditCards` PRIMARY KEY (`Id`),
                        CONSTRAINT `FK_CreditCards_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                    ) CHARACTER SET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();
            }

            // 1.1 Create Accounts table if it does not exist
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS `Accounts` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `UserId` int NOT NULL,
                        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
                        `Type` int NOT NULL DEFAULT 1,
                        `InitialBalance` decimal(18,2) NOT NULL DEFAULT 0.00,
                        `ColorHex` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT '#10B981',
                        `Icon` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'person',
                        `IsActive` tinyint(1) NOT NULL DEFAULT 1,
                        `CreatedAt` datetime(6) NOT NULL,
                        CONSTRAINT `PK_Accounts` PRIMARY KEY (`Id`),
                        CONSTRAINT `FK_Accounts_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                    ) CHARACTER SET=utf8mb4;";
                await cmd.ExecuteNonQueryAsync();
            }

            // 2. Ensure new columns in Transactions table
            await EnsureColumnAsync(conn, "Transactions", "CreditCardId", "int NULL");
            await EnsureColumnAsync(conn, "Transactions", "InstallmentNumber", "int NULL");
            await EnsureColumnAsync(conn, "Transactions", "TotalInstallments", "int NULL");
            await EnsureColumnAsync(conn, "Transactions", "InvoiceMonth", "int NULL");
            await EnsureColumnAsync(conn, "Transactions", "InvoiceYear", "int NULL");
            await EnsureColumnAsync(conn, "Transactions", "IsInvoicePayment", "tinyint(1) NOT NULL DEFAULT 0");
            await EnsureColumnAsync(conn, "Transactions", "RecurrenceGroupId", "varchar(50) NULL");
            await EnsureColumnAsync(conn, "Transactions", "AccountId", "int NULL");

            // 2.1 Ensure new columns in CreditCards table
            await EnsureColumnAsync(conn, "CreditCards", "AccountId", "int NULL");

            // 3. Ensure Index & Foreign keys
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) 
                    FROM information_schema.TABLE_CONSTRAINTS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                      AND TABLE_NAME = 'Transactions' 
                      AND CONSTRAINT_NAME = 'FK_Transactions_CreditCards_CreditCardId';";
                var fkCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (fkCount == 0)
                {
                    using var addFkCmd = conn.CreateCommand();
                    addFkCmd.CommandText = "ALTER TABLE `Transactions` ADD CONSTRAINT `FK_Transactions_CreditCards_CreditCardId` FOREIGN KEY (`CreditCardId`) REFERENCES `CreditCards` (`Id`) ON DELETE SET NULL;";
                    try { await addFkCmd.ExecuteNonQueryAsync(); } catch { }
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) 
                    FROM information_schema.TABLE_CONSTRAINTS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                      AND TABLE_NAME = 'Transactions' 
                      AND CONSTRAINT_NAME = 'FK_Transactions_Accounts_AccountId';";
                var fkCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (fkCount == 0)
                {
                    using var addFkCmd = conn.CreateCommand();
                    addFkCmd.CommandText = "ALTER TABLE `Transactions` ADD CONSTRAINT `FK_Transactions_Accounts_AccountId` FOREIGN KEY (`AccountId`) REFERENCES `Accounts` (`Id`) ON DELETE SET NULL;";
                    try { await addFkCmd.ExecuteNonQueryAsync(); } catch { }
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) 
                    FROM information_schema.TABLE_CONSTRAINTS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                      AND TABLE_NAME = 'CreditCards' 
                      AND CONSTRAINT_NAME = 'FK_CreditCards_Accounts_AccountId';";
                var fkCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (fkCount == 0)
                {
                    using var addFkCmd = conn.CreateCommand();
                    addFkCmd.CommandText = "ALTER TABLE `CreditCards` ADD CONSTRAINT `FK_CreditCards_Accounts_AccountId` FOREIGN KEY (`AccountId`) REFERENCES `Accounts` (`Id`) ON DELETE SET NULL;";
                    try { await addFkCmd.ExecuteNonQueryAsync(); } catch { }
                }
            }

            // 4. Automatic migration: Ensure each user has at least one account ("Conta Pessoal")
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO `Accounts` (`UserId`, `Name`, `Type`, `InitialBalance`, `ColorHex`, `Icon`, `IsActive`, `CreatedAt`)
                    SELECT u.`Id`, 'Conta Pessoal', 1, u.`InitialBalance`, '#10B981', 'person', 1, NOW()
                    FROM `Users` u
                    WHERE NOT EXISTS (SELECT 1 FROM `Accounts` a WHERE a.`UserId` = u.`Id`);";
                await cmd.ExecuteNonQueryAsync();
            }

            // 5. Backfill any existing transactions and credit cards with NULL AccountId
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE `Transactions` t
                    JOIN (
                        SELECT `UserId`, MIN(`Id`) AS `MinAccountId`
                        FROM `Accounts`
                        GROUP BY `UserId`
                    ) a ON a.`UserId` = t.`UserId`
                    SET t.`AccountId` = a.`MinAccountId`
                    WHERE t.`AccountId` IS NULL;";
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE `CreditCards` c
                    JOIN (
                        SELECT `UserId`, MIN(`Id`) AS `MinAccountId`
                        FROM `Accounts`
                        GROUP BY `UserId`
                    ) a ON a.`UserId` = c.`UserId`
                    SET c.`AccountId` = a.`MinAccountId`
                    WHERE c.`AccountId` IS NULL;";
                await cmd.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (!wasOpen)
            {
                await conn.CloseAsync();
            }
        }
    }

    private static async Task EnsureColumnAsync(System.Data.Common.DbConnection conn, string tableName, string columnName, string columnDefinition)
    {
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = $@"
            SELECT COUNT(*) 
            FROM information_schema.COLUMNS 
            WHERE TABLE_SCHEMA = DATABASE() 
              AND TABLE_NAME = '{tableName}' 
              AND COLUMN_NAME = '{columnName}';";

        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        if (count == 0)
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE `{tableName}` ADD `{columnName}` {columnDefinition};";
            await alterCmd.ExecuteNonQueryAsync();
        }
    }
}
