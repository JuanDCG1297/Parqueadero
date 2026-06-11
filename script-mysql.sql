-- ============================================================
-- Script SQL para MySQL — Parqueadero API
-- Requiere MySQL 8.0.13+ (por el filtered index)
-- ============================================================

CREATE DATABASE IF NOT EXISTS `parqueadero`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `parqueadero`;

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;

-- Tabla de tipos de vehículo (catálogo)
CREATE TABLE `VehicleTypes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(10) NOT NULL,
    PRIMARY KEY (`Id`)
);

INSERT INTO `VehicleTypes` (`Id`, `Name`) VALUES (1, 'Carro'), (2, 'Moto');

-- Tabla de entradas/salidas de vehículos
CREATE TABLE `VehicleEntries` (
    `Id` char(36) NOT NULL,
    `VehicleType` int NOT NULL,
    `Plate` varchar(10) NOT NULL,
    `EntryTime` datetime(6) NOT NULL,
    `ExitTime` datetime(6) NULL,
    `TotalMinutes` int NULL,
    `Fee` decimal(18,2) NULL,
    `EmailSent` tinyint(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_VehicleEntries_VehicleTypes`
        FOREIGN KEY (`VehicleType`) REFERENCES `VehicleTypes` (`Id`)
);

-- Filtered unique index: evita placas duplicadas activas (sin fecha de salida)
-- Requiere MySQL 8.0.13+
CREATE UNIQUE INDEX `IX_VehicleEntry_Plate_Active`
    ON `VehicleEntries` (`Plate`)
    WHERE `ExitTime` IS NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260610002831_InitialCreate', '10.0.9');

COMMIT;
