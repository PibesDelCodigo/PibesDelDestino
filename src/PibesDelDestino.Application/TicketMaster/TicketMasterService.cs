using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.TicketMaster
{
    public class TicketMasterService : ApplicationService, ITicketMasterService
    {
        private readonly IEventSearchService _eventSearchService;

        public TicketMasterService(IEventSearchService eventSearchService)
        {
            _eventSearchService = eventSearchService;
        }

        public async Task<List<EventoDTO>> SearchEventsAsync(string cityName)
        {
            // Delegamos la responsabilidad al servicio de infraestructura
            return await _eventSearchService.SearchEventsAsync(cityName);
        }
    }
}