using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


//Tickemaster no devuelve una lista simple, sino un objeto complejo,
// tiene lista de eventos dentro de una propiedad "_embedded".
namespace PibesDelDestino.TicketMaster
{
    public class TicketMasterRoot
    {
        [JsonPropertyName("_embedded")]
        public TicketMasterEmbedded Embedded { get; set; }
    }

    public class TicketMasterEmbedded
    {
        [JsonPropertyName("events")]
        public List<TicketMasterEvent> Events { get; set; }
    }

    //Usamos JsonPropertyName para mapear las propiedades JSON a las propiedades C# ya que utiliza minusculas y C# mayusculas.
    //Definimos como es la estructura de un evento en TicketMaster

    public class TicketMasterEvent
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("images")]
        public List<TicketMasterImage> Images { get; set; }

        [JsonPropertyName("dates")]
        public TicketMasterDates Dates { get; set; }
    }

    //Definimos como es la estructura de una imagen en TicketMaster
    public class TicketMasterImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    //Definimos como es la estructura de las fechas en TicketMaster

    public class TicketMasterDates
    {
        [JsonPropertyName("start")]
        public TicketMasterStart Start { get; set; }
    }

    //Definimos como es la estructura del inicio de un evento en TicketMaster

    public class TicketMasterStart
    {
        [JsonPropertyName("localDate")]
        public string LocalDate { get; set; } // Viene como "2026-02-10"
    }

}