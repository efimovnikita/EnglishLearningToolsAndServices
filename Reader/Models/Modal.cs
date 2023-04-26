namespace Reader.Models;

public class Modal
{
    public string? Title { get; set; } = "";
    public string? TextContent { get; set; } = "";
    public ModalType ModalType { get; set; } = ModalType.Regular;
}

public enum ModalType
{
    Regular, Warning, Error
}