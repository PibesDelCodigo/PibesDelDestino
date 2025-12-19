
export interface TranslateDto {
  textToTranslate: string;
  targetLanguage?: string;
}

export interface TranslationResultDto {
  translatedText?: string;
}
