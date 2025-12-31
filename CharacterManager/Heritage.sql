UPDATE Pieces SET Discriminator = 'Piece' WHERE Discriminator IS NULL OR Discriminator = '';
UPDATE Personnages SET Discriminator = 'Personnage' WHERE Discriminator IS NULL OR Discriminator = '';