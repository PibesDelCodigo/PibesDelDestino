using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Values;

namespace PibesDelDestino.Destinations
{
    public class Coordinates : ValueObject
    {
        public float Latitude { get; private set; }
        public float Longitude { get; private set; }

        private Coordinates() { } // EF Core

        public Coordinates(
            float latitude,
            float longitude)
        {
            if (!ValidateRange(latitude, longitude))
            {
                throw new ArgumentException("Invalid latitude or longitude values.");
            }
            Latitude = latitude;
            Longitude = longitude;
        }

        public bool ValidateRange()
        {
            return ValidateRange(Latitude, Longitude);
        }

        private static bool ValidateRange(float latitude, float longitude)
        {
            return latitude >= -90 && latitude <= 90 &&
                   longitude >= -180 && longitude <= 180;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Latitude;
            yield return Longitude;
        }
    }
}