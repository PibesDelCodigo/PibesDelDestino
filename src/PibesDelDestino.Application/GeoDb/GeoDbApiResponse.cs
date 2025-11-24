using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PibesDelDestino.GeoDb
{
    public class GeoDbApiResponse
    {
        [JsonPropertyName("data")]
        public List<GeoDbCity> Data { get; set; }
    }

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