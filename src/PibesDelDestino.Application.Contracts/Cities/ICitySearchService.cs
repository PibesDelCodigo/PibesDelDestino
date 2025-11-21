using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PibesDelDestino.Cities
{
    public interface ICitySearchService
    {
    Task <CityResultDto> SearchCitiesAsync(CityRequestDTO request);
    }
}
