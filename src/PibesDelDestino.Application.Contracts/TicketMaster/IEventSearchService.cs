using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PibesDelDestino.TicketMaster
{
    public interface IEventSearchService
    {
        Task<List<EventoDTO>> SearchEventsAsync(string cityName);
    }
}
