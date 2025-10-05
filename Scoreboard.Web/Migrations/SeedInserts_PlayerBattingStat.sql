-- Ejemplo de INSERTs para PlayerBattingStats (ajusta Ids de Jugador y Partido según tu BD)
INSERT INTO `PlayerBattingStats` (`JugadorId`,`PartidoId`,`Fecha`,`AB`,`R`,`H`,`Doubles`,`Triples`,`HR`,`RBI`,`BB`,`SO`,`HBP`,`SF`)
VALUES
(1, 1, '2023-05-10 00:00:00', 4, 1, 2, 1, 0, 0, 1, 0, 1, 0, 0),
(1, 3, '2024-04-12 00:00:00', 3, 2, 1, 0, 0, 1, 2, 1, 0, 0, 0),
(2, 2, '2023-06-15 00:00:00', 4, 0, 3, 0, 0, 0, 2, 0, 0, 0, 0),
(3, 4, '2024-06-20 00:00:00', 5, 1, 2, 1, 0, 0, 1, 1, 1, 0, 0);

-- Nota: reemplaza los valores de JugadorId y PartidoId con los que existan en tu BD.
