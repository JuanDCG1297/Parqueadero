CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "VehicleEntries" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_VehicleEntries" PRIMARY KEY,
    "VehicleType" TEXT NOT NULL,
    "Plate" TEXT NOT NULL,
    "EntryTime" TEXT NOT NULL,
    "ExitTime" TEXT NULL,
    "TotalMinutes" INTEGER NULL,
    "Fee" decimal(18,2) NULL,
    "EmailSent" INTEGER NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX "IX_VehicleEntry_Plate_Active" ON "VehicleEntries" ("Plate") WHERE [ExitTime] IS NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260610002831_InitialCreate', '10.0.9');

COMMIT;

