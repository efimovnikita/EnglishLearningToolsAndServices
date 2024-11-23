using opennlp.tools.langdetect;
using opennlp.tools.sentdetect;
using opennlp.tools.tokenize;

namespace Reader.Models;

public class TokenizerAndSentenceExtractor
{
    public static List<Sentence> GetSentencesList(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input text cannot be null or empty");
        }

        // Detect language first
        var langDetectModelIn = new java.io.FileInputStream("langdetect-183.bin");
        var languageDetector = new LanguageDetectorModel(langDetectModelIn);
        var languageDetectorMe = new LanguageDetectorME(languageDetector);
        var language = languageDetectorMe.predictLanguage(input);
        var lang = language.getLang();

        if (string.IsNullOrEmpty(lang))
        {
            throw new InvalidOperationException("Could not detect language");
        }

        // Select appropriate model files based on language
        string sentenceModelPath;
        string tokenizerModelPath;

        switch (lang.ToLower())
        {
            case "eng":
                sentenceModelPath = "opennlp-en-ud-ewt-sentence-1.0-1.9.3.bin";
                tokenizerModelPath = "opennlp-en-ud-ewt-tokens-1.0-1.9.3.bin";
                break;
            case "ita":
                sentenceModelPath = "opennlp-it-ud-vit-sentence-1.0-1.9.3.bin";
                tokenizerModelPath = "opennlp-it-ud-vit-tokens-1.0-1.9.3.bin";
                break;
            default:
                throw new NotSupportedException($"Language '{lang}' is not supported. Please use English or Italian.");
        }

        // Load appropriate models
        java.io.InputStream sentenceModelIn = new java.io.FileInputStream(sentenceModelPath);
        java.io.InputStream tokensModelIn = new java.io.FileInputStream(tokenizerModelPath);
        
        SentenceModel sentenceModel = new(sentenceModelIn);
        TokenizerModel tokenizerModel = new(tokensModelIn);
        SentenceDetectorME sentenceDetector = new(sentenceModel);
        Tokenizer tokenizer = new TokenizerME(tokenizerModel);

        string[] sentences = sentenceDetector.sentDetect(input);

        List<Sentence> sentencesList = new();
        foreach (string sentence in sentences)
        {
            string[]? tokens = tokenizer.tokenize(sentence);
            Sentence sentenceObj = new() {Id = Guid.NewGuid(), Tokens = new List<Token>()};
            foreach (string token in tokens)
            {
                sentenceObj.Tokens.Add(new Token {Id = Guid.NewGuid(), SentenceId = sentenceObj.Id, Text = token});
            }

            sentencesList.Add(sentenceObj);
        }

        return sentencesList;
    }
}