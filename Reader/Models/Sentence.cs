namespace Reader.Models;

public class Sentence
{
    public Guid Id { get; set; }
    public List<Token> Tokens { get; set; } = new();
    public List<Token> GetTokensWithSpaces()
    {
        List<Token> result = new();
        for (int i = 0; i < Tokens.Count; i++)
        {
            Token token = Tokens[i];
            result.Add(token);

            if (Tokens.Count <= 1)
            {
                continue;
            }

            Token preLast = Tokens[^2];

            Token? nextToken = GetNextToken(i, this);

            if (token.Equals(preLast))
            {
                continue;
            }

            if (IsSpaceNeeded(nextToken))
            {
                result.Add(new Token {Text = " ", Id = Guid.NewGuid(), Definition = "", SentenceId = this.Id});
            }
        }

        return result;
    }

    public string GetJoinedTokensWithSpaces() => String.Join("", GetTokensWithSpaces().Select(token => token.Text));

    private static Token? GetNextToken(int i, Sentence sentence)
    {
        Token? nextToken = null;
        if (i < sentence.Tokens.Count - 1)
        {
            nextToken = sentence.Tokens[i + 1];
        }

        return nextToken;
    }
    
    private static bool IsSpaceNeeded(Token? nextToken)
    {
        return nextToken != null && 
               nextToken.Text.Equals(",") == false &&
               nextToken.Text.Equals(":") == false &&
               nextToken.Text.Equals(";") == false;
    }
}