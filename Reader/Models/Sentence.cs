namespace Reader.Models;

public class Sentence
{
    public Guid Id { get; set; }
    public List<Token> Tokens { get; set; }
}