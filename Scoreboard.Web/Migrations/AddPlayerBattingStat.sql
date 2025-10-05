START TRANSACTION;
CREATE TABLE `PlayerBattingStats` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `JugadorId` int NOT NULL,
    `PartidoId` int NOT NULL,
    `Fecha` datetime(6) NOT NULL,
    `AB` int NOT NULL,
    `R` int NOT NULL,
    `H` int NOT NULL,
    `Doubles` int NOT NULL,
    `Triples` int NOT NULL,
    `HR` int NOT NULL,
    `RBI` int NOT NULL,
    `BB` int NOT NULL,
    `SO` int NOT NULL,
    `HBP` int NOT NULL,
    `SF` int NOT NULL,
    CONSTRAINT `PK_PlayerBattingStats` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PlayerBattingStats_Jugadores_JugadorId` FOREIGN KEY (`JugadorId`) REFERENCES `Jugadores` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_PlayerBattingStats_JugadorId` ON `PlayerBattingStats` (`JugadorId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251005195856_AddPlayerBattingStat', '9.0.8');

COMMIT;

