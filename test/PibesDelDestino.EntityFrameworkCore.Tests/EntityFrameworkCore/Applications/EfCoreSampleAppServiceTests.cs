using PibesDelDestino.Samples;
using Xunit;

namespace PibesDelDestino.EntityFrameworkCore.Applications;

[Collection(PibesDelDestinoTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<PibesDelDestinoEntityFrameworkCoreTestModule>
{

}
