using System.ComponentModel.DataAnnotations;

namespace PibesDelDestino.Translation
{
    public class TranslateDto
    {
        [Required]
        public string TextToTranslate { get; set; }
        public string TargetLanguage { get; set; } = "en"; // Por defecto a Inglés
    }
}