using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scoreboard.Web.Datos;
using Scoreboard.Web.Modelos;
using Scoreboard.Web.Servicios.Marcador;
using System.Data.Common;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var webPath = Path.Combine(root, "Scoreboard.Web");

var configuration = new ConfigurationBuilder()
	.AddJsonFile(Path.Combine(webPath, "appsettings.json"), optional: false)
	.AddJsonFile(Path.Combine(webPath, "appsettings.Development.json"), optional: true)
	.AddEnvironmentVariables()
	.Build();

var connectionString = configuration.GetConnectionString("PorDefecto")
	?? Environment.GetEnvironmentVariable("ConnectionStrings__PorDefecto")
	?? throw new InvalidOperationException("No se encontró la cadena de conexión 'PorDefecto'.");

var optionsBuilder = new DbContextOptionsBuilder<ContextoMarcador>();
optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

using var db = new ContextoMarcador(optionsBuilder.Options);
var loggerFactory = LoggerFactory.Create(builder =>
{
	builder.AddSimpleConsole(o =>
	{
		o.SingleLine = true;
		o.TimestampFormat = "HH:mm:ss ";
	});
	builder.SetMinimumLevel(LogLevel.Information);
});

var marcador = new MarcadorService(db, loggerFactory.CreateLogger<MarcadorService>(), null);
var rng = new Random();

await db.Database.EnsureCreatedAsync();
await EnsureColumnAsync(db, "PlayLogs", "Kind", "`Kind` int NOT NULL DEFAULT 0");
await EnsureColumnAsync(db, "PlayLogs", "CreadoUtc", "`CreadoUtc` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)");

var lastMatch = await db.Partidos
	.Include(p => p.EquipoCasa)
	.Include(p => p.EquipoVisita)
	.OrderByDescending(p => p.Fecha)
	.FirstOrDefaultAsync();

if (lastMatch == null)
{
	Console.WriteLine("No existen partidos previos para determinar rivales. Crea datos básicos antes de ejecutar el simulador.");
	return;
}

var casaId = lastMatch.EquipoCasaId;
var visitaId = lastMatch.EquipoVisitaId;

var jugadoresCasa = await EnsurePlayersAsync(db, casaId, "Casa", rng);
var jugadoresVisita = await EnsurePlayersAsync(db, visitaId, "Visita", rng);

var nuevoPartido = new Partido
{
	Fecha = DateTime.Now,
	EquipoCasaId = casaId,
	EquipoVisitaId = visitaId,
	Estado = EstadoPartido.NoIniciado,
	EntradaActual = 1,
	Mitad = MitadEntrada.Alta,
	CarrerasCasa = 0,
	CarrerasVisita = 0
};

db.Partidos.Add(nuevoPartido);
await db.SaveChangesAsync();

await CrearLineupAsync(db, nuevoPartido.Id, casaId, jugadoresCasa, rng);
await CrearLineupAsync(db, nuevoPartido.Id, visitaId, jugadoresVisita, rng);
await db.SaveChangesAsync();

await marcador.IniciarPartidoAsync(nuevoPartido.Id);

var resultados = Enum.GetValues<ResultadoTurno>();
var totalJugadas = rng.Next(12, 20);

Console.WriteLine($"Registrando {totalJugadas} jugadas aleatorias para el partido {nuevoPartido.Id}...");

for (var jugada = 0; jugada < totalJugadas; jugada++)
{
	var estado = await db.Partidos.AsNoTracking().FirstAsync(p => p.Id == nuevoPartido.Id);
	var casaBatea = estado.Mitad == MitadEntrada.Baja;
	var lineupActual = await db.Lineups.AsNoTracking()
		.Where(l => l.PartidoId == nuevoPartido.Id && l.EquipoId == (casaBatea ? casaId : visitaId))
		.OrderBy(l => l.Orden)
		.ToListAsync();

	if (!lineupActual.Any())
	{
		Console.WriteLine("No hay lineup para el equipo que batea; abortando simulación.");
		break;
	}

	var idx = casaBatea ? estado.IndexBateadorCasa ?? 0 : estado.IndexBateadorVisita ?? 0;
	idx = Math.Clamp(idx, 0, lineupActual.Count - 1);
	var jugadorId = lineupActual[idx].JugadorId;
	var resultado = resultados[rng.Next(resultados.Length)];

	try
	{
		await marcador.RegistrarTurnoAsync(nuevoPartido.Id, jugadorId, resultado);
		Console.WriteLine($"Turno {jugada + 1}: {(casaBatea ? "CASA" : "VISITA")} jugador {jugadorId} => {resultado}");
	}
	catch (Exception ex)
	{
		var detail = ex.InnerException?.Message ?? string.Empty;
		Console.WriteLine($"Error registrando turno: {ex.Message} {detail}");
		break;
	}
}

var final = await db.Partidos.AsNoTracking()
	.Include(p => p.EquipoCasa)
	.Include(p => p.EquipoVisita)
	.FirstAsync(p => p.Id == nuevoPartido.Id);

Console.WriteLine($"Simulación completada. Partido #{final.Id}: {final.EquipoVisita?.Nombre} {final.CarrerasVisita} - {final.CarrerasCasa} {final.EquipoCasa?.Nombre}");
Console.WriteLine($"Entrada actual: {final.EntradaActual} {final.Mitad}, Outs: {final.Outs}");

static async Task EnsureColumnAsync(ContextoMarcador db, string table, string column, string definitionSql)
{
	if (await ColumnExistsAsync(db, table, column)) return;
	var sql = $"ALTER TABLE `{table}` ADD COLUMN {definitionSql};";
	await db.Database.ExecuteSqlRawAsync(sql);
	Console.WriteLine($"Columna {table}.{column} creada.");
}

static async Task<bool> ColumnExistsAsync(ContextoMarcador db, string table, string column)
{
	var connection = db.Database.GetDbConnection();
	var shouldClose = connection.State != System.Data.ConnectionState.Open;
	if (shouldClose)
		await connection.OpenAsync();

	await using var command = connection.CreateCommand();
	command.CommandText = "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @column";
	var tableParam = command.CreateParameter();
	tableParam.ParameterName = "@table";
	tableParam.Value = table;
	command.Parameters.Add(tableParam);
	var columnParam = command.CreateParameter();
	columnParam.ParameterName = "@column";
	columnParam.Value = column;
	command.Parameters.Add(columnParam);

	var result = (long)(await command.ExecuteScalarAsync() ?? 0);

	if (shouldClose)
		await connection.CloseAsync();

	return result > 0;
}

static async Task<List<Jugador>> EnsurePlayersAsync(ContextoMarcador db, int equipoId, string prefijo, Random rng)
{
	var jugadores = await db.Jugadores.Where(j => j.EquipoId == equipoId).ToListAsync();
	var nextNumber = jugadores.Count == 0 ? rng.Next(1, 60) : jugadores.Max(j => j.NumeroUniforme) + 1;
	while (jugadores.Count < 9)
	{
		var nuevo = new Jugador
		{
			Nombre = $"{prefijo}{jugadores.Count + 1}",
			Apellido = "Auto",
			NumeroUniforme = nextNumber++,
			Posicion = Posicion.Utility,
			EquipoId = equipoId
		};
		jugadores.Add(nuevo);
		db.Jugadores.Add(nuevo);
	}
	await db.SaveChangesAsync();
	return jugadores;
}

static async Task CrearLineupAsync(ContextoMarcador db, int partidoId, int equipoId, List<Jugador> jugadores, Random rng)
{
	var existentes = await db.Lineups.Where(l => l.PartidoId == partidoId && l.EquipoId == equipoId).ToListAsync();
	if (existentes.Any()) db.Lineups.RemoveRange(existentes);

	var seleccion = jugadores
		.OrderBy(_ => rng.Next())
		.Take(Math.Min(9, jugadores.Count))
		.ToList();

	var orden = 1;
	foreach (var jugador in seleccion)
	{
		db.Lineups.Add(new LineupItem
		{
			PartidoId = partidoId,
			EquipoId = equipoId,
			JugadorId = jugador.Id,
			Orden = orden++
		});
	}
}
