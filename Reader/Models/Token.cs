namespace Reader.Models;

public class Token
{
    public Guid Id { get; set; }
    public Guid SentenceId { get; set; }
    public string Text { get; set; }
    public string Definition { get; set; }
}