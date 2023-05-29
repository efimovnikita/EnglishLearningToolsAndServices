using TiktokenSharp;

namespace Reader.Models;

public class Fraction
{
    private readonly TikToken _tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");

    public Fraction(List<Sentence> sentences)
    {
        Sentences = sentences;
    }
    public List<Sentence> Sentences { get; set; }
    public override string ToString() => String.Join(" ", Sentences.Select(sentence => sentence.ToString()));

    public bool IsSatisfiesLimit(int limit)
    {
        return _tikToken.Encode(ToString()).Count < limit;
    }
    
    public List<Fraction> SplitIntoSubfractions(int limit)
    {
        List<Fraction> subFractions = new();
        List<Sentence> currentSentences = new();

        foreach (Sentence sentence in Sentences)
        {
            // Check if a single sentence exceeds the limit
            Fraction singleSentenceFraction = new(new List<Sentence> { sentence });
            if (!singleSentenceFraction.IsSatisfiesLimit(limit))
            {
                throw new InvalidOperationException("A single sentence exceeds the given limit.");
            }

            currentSentences.Add(sentence);
            Fraction currentFraction = new(currentSentences);

            if (currentFraction.IsSatisfiesLimit(limit))
            {
                continue;
            }

            currentSentences.Remove(sentence); // remove last sentence, it exceeded the limit
                
            // add previously collected sentences as a new Fraction
            subFractions.Add(new Fraction(new List<Sentence>(currentSentences)));
                
            // start new Fraction with the current sentence
            currentSentences.Clear();
            currentSentences.Add(sentence);
        }

        // add remaining sentences as the last Fraction
        if (currentSentences.Count > 0)
        {
            subFractions.Add(new Fraction(currentSentences));
        }

        return subFractions;
    }
}