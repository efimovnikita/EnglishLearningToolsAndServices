namespace Reader.Models;

public class RequestTooLongException : Exception
{
    public RequestTooLongException(string? message) : base(message)
    {
    }
}