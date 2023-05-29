using opennlp.tools.sentdetect;
using opennlp.tools.tokenize;

namespace Reader.Models;

public class TokenizerAndSentenceExtractor
{
    public static List<Sentence> GetSentencesList(string input)
    {
        java.io.InputStream sentenceModelIn = new java.io.FileInputStream("opennlp-en-ud-ewt-sentence-1.0-1.9.3.bin");
        java.io.InputStream tokensModelIn = new java.io.FileInputStream("opennlp-en-ud-ewt-tokens-1.0-1.9.3.bin");
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