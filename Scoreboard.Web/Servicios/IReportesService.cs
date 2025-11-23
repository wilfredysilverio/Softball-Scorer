using System.Threading.Tasks;

namespace Scoreboard.Web.Servicios
{
    public interface IReportesService
    {
        Task<byte[]> GenerarBoxScoreCsvAsync(int partidoId);
        Task<byte[]> GenerarPlayByPlayCsvAsync(int partidoId);
    }
}
