using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

//Vamos a definir las interfaces para el servicio de TicketMaster utilizando un Evento Dto

namespace PibesDelDestino.TicketMaster
{
    public interface ITicketMasterService : ITransientDependency
    {

        Task<List<EventoDTO>> SearchEventsAsync(string cityName);

    }
}