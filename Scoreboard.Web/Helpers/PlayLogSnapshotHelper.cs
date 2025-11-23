using System.Text.Json;
using Scoreboard.Web.Modelos;

namespace Scoreboard.Web.Helpers
{
    internal static class PlayLogSnapshotHelper
    {
        public static (SnapshotPartido? Before, SnapshotPartido? After) ExtractSnapshots(string? snapshotJson)
        {
            if (string.IsNullOrWhiteSpace(snapshotJson))
                return (null, null);

            try
            {
                var wrapper = JsonSerializer.Deserialize<PlaySnapshotWrapper>(snapshotJson);
                if (wrapper?.Before != null || wrapper?.After != null)
                {
                    return (wrapper?.Before, wrapper?.After);
                }

                var single = JsonSerializer.Deserialize<SnapshotPartido>(snapshotJson);
                return (single, single);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
