CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Clients" (
    "Id" uuid NOT NULL,
    "Nom" text NOT NULL,
    "Telephone" text NOT NULL,
    "Type" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Clients" PRIMARY KEY ("Id")
);

CREATE TABLE "Services" (
    "Id" uuid NOT NULL,
    "Nom" text NOT NULL,
    "Prix" numeric NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Services" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260128202939_AddServices', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE "Devis" (
    "Id" uuid NOT NULL,
    "ClientId" uuid NOT NULL,
    "Total" numeric NOT NULL,
    "Statut" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Devis" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Devis_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE CASCADE
);

CREATE TABLE "DevisItems" (
    "Id" uuid NOT NULL,
    "DevisId" uuid NOT NULL,
    "ServiceItemId" uuid NOT NULL,
    "Quantite" integer NOT NULL,
    "PrixUnitaire" numeric NOT NULL,
    "Total" numeric NOT NULL,
    CONSTRAINT "PK_DevisItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DevisItems_Devis_DevisId" FOREIGN KEY ("DevisId") REFERENCES "Devis" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DevisItems_Services_ServiceItemId" FOREIGN KEY ("ServiceItemId") REFERENCES "Services" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Devis_ClientId" ON "Devis" ("ClientId");

CREATE INDEX "IX_DevisItems_DevisId" ON "DevisItems" ("DevisId");

CREATE INDEX "IX_DevisItems_ServiceItemId" ON "DevisItems" ("ServiceItemId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260128213157_AddDevis', '8.0.2');

COMMIT;

START TRANSACTION;

CREATE TABLE "Factures" (
    "Id" uuid NOT NULL,
    "DevisId" uuid NOT NULL,
    "Numero" text NOT NULL,
    "Total" numeric NOT NULL,
    "Statut" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Factures" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Factures_Devis_DevisId" FOREIGN KEY ("DevisId") REFERENCES "Devis" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Factures_DevisId" ON "Factures" ("DevisId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260128214032_AddFactures', '8.0.2');

COMMIT;

