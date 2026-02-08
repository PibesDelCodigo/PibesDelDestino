using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PibesDelDestino.Metrics
{
    public class DashboardDto
    {
        // Estadísticas Técnicas
        public int TotalApiCalls { get; set; }
        public double SuccessRate { get; set; }
        public double AvgResponseTime { get; set; }

        // Estadísticas de Negocio (Top Búsquedas)
        public Dictionary<string, int> TopSearches { get; set; }
    }
}