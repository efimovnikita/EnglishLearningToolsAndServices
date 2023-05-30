using System.Text;
using TiktokenSharp;

namespace Reader.Models;

public class RequestManager
{
    public RequestManager(List<Page> pages, Token selectedToken)
    {
        SelectedToken = selectedToken;
        Sentences = new LinkedList<Sentence>(pages.SelectMany(page => page.Sentences));
        MainContextSentence = Sentences.FirstOrDefault(sentence => sentence.Tokens.Contains(selectedToken));
        if (MainContextSentence == null)
        {
            throw new Exception("Main context sentence wasn't found");
        }
    }

    public Token SelectedToken { get; set; }

    public string GetBaseRequestForWord()
    {
        StringBuilder builder = new();
        builder.AppendLine($"Give me a simple definition for the word \'{SelectedToken.Text}\' in the context of the sentence \'{MainContextSentence!.ToString()}\'.");
        if (TikToken.Encode(builder.ToString()).Count > TokenLimit)
        {
            throw new RequestTooLongException("Request is too long. Try select another word.");
        }

        LinkedListNode<Sentence>? mainSentence = Sentences.Find(MainContextSentence);
        if (mainSentence == null)
        {
            throw new Exception("Main sentence wasn't found");
        }

        LinkedListNode<Sentence>? sentencePrevious = mainSentence.Previous;
        if (sentencePrevious != null)
        {
            if (TikToken.Encode(sentencePrevious.ToString()!).Count + TikToken.Encode(builder.ToString()).Count < TokenLimit)
            {
                builder.AppendLine($"Take into account that previous sentence is '{sentencePrevious.Value.ToString()}'.");
            }
        }

        LinkedListNode<Sentence>? sentenceNext = mainSentence.Next;
        if (sentenceNext != null)
        {
            if (TikToken.Encode(sentenceNext.ToString()!).Count + TikToken.Encode(builder.ToString()).Count < TokenLimit)
            {
                builder.AppendLine($"Next sentence is '{sentenceNext.Value.ToString()}'.");
            }
        }

        return builder.ToString();
    }

    public string GetSynonymsRequestForWord()
    {
        StringBuilder builder = new();
        builder.AppendLine($"Give me a list of 3 most appropriate synonyms for word \'{SelectedToken.Text}\' in the context of the sentence \'{MainContextSentence!.ToString()}\'. Split synonyms by comma. Follow the pattern: \'Synonyms: list of synonyms.\'");
        if (TikToken.Encode(builder.ToString()).Count > TokenLimit)
        {
            throw new RequestTooLongException("Synonyms request is too long. Try select another word.");
        }
        
        return builder.ToString();
    }

    public static string GetRequestForSentences(TikToken tikToken, List<Sentence> sentences,
        string? promptStarter = "Paraphrase this sentences and use really simple English words:\n\n")
    {
        StringBuilder builder = new();
        builder.AppendLine(promptStarter);
        foreach (Sentence sentence in sentences)
        {
            builder.AppendLine(sentence.ToString());
        }
        
        if (tikToken.Encode(builder.ToString()).Count > TokenLimit)
        {
            throw new RequestTooLongException("Request is too long. Try to reduce number of sentences.");
        }
        
        return builder.ToString();
    }

    public Sentence? MainContextSentence { get; set; }

    public LinkedList<Sentence> Sentences { get; set; }
    public TikToken TikToken { get; } = TikToken.EncodingForModel("gpt-3.5-turbo");
    private const int TokenLimit = 3000;
}