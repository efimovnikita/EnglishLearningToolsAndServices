using TiktokenSharp;

namespace Reader.Models;

public class Fraction
{
    public Fraction(List<Sentence> sentences)
    {
        Sentences = sentences;
    }
    public List<Sentence> Sentences { get; set; }
    public override string ToString() => String.Join(" ", Sentences.Select(sentence => sentence.ToString()));

    public bool IsSatisfiesLimit(int limit)
    {
        TikToken tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");
        return tikToken.Encode(ToString()).Count < limit;
    }
}