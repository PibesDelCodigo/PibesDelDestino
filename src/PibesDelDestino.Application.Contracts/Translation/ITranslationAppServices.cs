using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Translation
{
    public interface ITranslationAppService : IApplicationService
    {
        Task<TranslationResultDto> TranslateAsync(TranslateDto input);
    }
}