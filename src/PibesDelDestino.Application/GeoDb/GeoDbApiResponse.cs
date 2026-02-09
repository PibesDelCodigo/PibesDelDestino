using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PibesDelDestino.GeoDb
{
    public class GeoDbApiResponse
    {
        [JsonPropertyName("data")]
        public List<GeoDbCity> Data { get; set; }
    }
    //"Estas clases actúan como un espejo de la respuesta JSON que nos
    //envía la API externa de GeoDB. Sirven para
    //convertir el texto JSON plano, en objetos C# tipados y manipulables."

    public class GeoDbCity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("latitude")]
        public float Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public float Longitude { get; set; }

        [JsonPropertyName("population")]
        public int Population { get; set; }
    }
}