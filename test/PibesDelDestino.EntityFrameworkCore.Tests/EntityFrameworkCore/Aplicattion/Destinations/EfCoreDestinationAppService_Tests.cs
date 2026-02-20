using PibesDelDestino.Destinations;
using Xunit;

namespace PibesDelDestino.EntityFrameworkCore.Applications.Destinations
{
    [Collection(PibesDelDestinoTestConsts.CollectionDefinitionName)]
    public class EfCoreDestinationAppService_Tests : DestinationAppService_Tests<PibesDelDestinoEntityFrameworkCoreTestModule>
    {

    }
}
