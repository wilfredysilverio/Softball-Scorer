using System.Threading.Tasks;

namespace Scoreboard.Web.Servicios
{
    public interface IExcelExportService
    {
        Task<(byte[] contenido, string fileName)> BuildPlayByPlayXlsxAsync(int partidoId);
    }
}
