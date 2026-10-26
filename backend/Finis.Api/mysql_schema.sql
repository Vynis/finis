CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `Users` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `InitialBalance` decimal(18,2) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Categories` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Type` int NOT NULL,
    `ColorHex` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Icon` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `IsDefault` tinyint(1) NOT NULL,
    `UserId` int NULL,
    CONSTRAINT `PK_Categories` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Categories_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

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
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Transactions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `Date` datetime(6) NOT NULL,
    `Type` int NOT NULL,
    `Amount` decimal(18,2) NOT NULL,
    `Description` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Status` int NOT NULL,
    `PaymentMethod` int NOT NULL,
    `Notes` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CategoryId` int NULL,
    `CreditCardId` int NULL,
    `InstallmentNumber` int NULL,
    `TotalInstallments` int NULL,
    `InvoiceMonth` int NULL,
    `InvoiceYear` int NULL,
    `IsInvoicePayment` tinyint(1) NOT NULL DEFAULT 0,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Transactions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Transactions_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_Transactions_CreditCards_CreditCardId` FOREIGN KEY (`CreditCardId`) REFERENCES `CreditCards` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_Transactions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX IF NOT EXISTS `IX_Categories_UserId` ON `Categories` (`UserId`);
CREATE INDEX IF NOT EXISTS `IX_Transactions_CategoryId` ON `Transactions` (`CategoryId`);
CREATE INDEX IF NOT EXISTS `IX_Transactions_UserId_Date` ON `Transactions` (`UserId`, `Date`);
CREATE INDEX IF NOT EXISTS `IX_Transactions_CreditCardId` ON `Transactions` (`CreditCardId`);
CREATE INDEX IF NOT EXISTS `IX_CreditCards_UserId` ON `CreditCards` (`UserId`);
CREATE UNIQUE INDEX IF NOT EXISTS `IX_Users_Email` ON `Users` (`Email`);

COMMIT;
