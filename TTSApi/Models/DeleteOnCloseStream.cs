namespace TTSApi.Models;

public class DeleteOnCloseStream : FileStream
{
    private readonly ILogger<DeleteOnCloseStream> _logger;

    public DeleteOnCloseStream(ILogger<DeleteOnCloseStream> logger, string path, FileMode mode)
        : base(path, mode)
    {
        _logger = logger;
        _logger.LogInformation("A new DeleteOnCloseStream has been instantiated with path {Path} and mode {Mode}",
            path, mode);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        try
        {
            File.Delete(Name);
            _logger.LogInformation("The file {FileName} has been deleted on Dispose", Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting the file {FileName} on Dispose", Name);
        }
    }
}