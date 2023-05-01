namespace Reader.Models;

public class Page
{
    public int PageNumber { get; set; }
    public List<Sentence> Sentences { get; set; } = new();
}