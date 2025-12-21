-- Création des tables pour LucieHouse
CREATE TABLE IF NOT EXISTS "LucieHouses" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_LucieHouses" PRIMARY KEY AUTOINCREMENT
);

CREATE TABLE IF NOT EXISTS "Pieces" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Pieces" PRIMARY KEY AUTOINCREMENT,
    "Nom" TEXT NOT NULL,
    "Niveau" INTEGER NOT NULL,
    "Puissance" INTEGER NOT NULL,
    "Selectionnee" INTEGER NOT NULL,
    "BonusTactiques" TEXT NOT NULL,
    "BonusStrategiques" TEXT NOT NULL,
    "AspectsTactiques" TEXT NOT NULL,
    "AspectsStrategiques" TEXT NOT NULL,
    "LucieHouseId" INTEGER,
    CONSTRAINT "FK_Pieces_LucieHouses_LucieHouseId" FOREIGN KEY ("LucieHouseId") 
        REFERENCES "LucieHouses" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Pieces_LucieHouseId" ON "Pieces" ("LucieHouseId");
