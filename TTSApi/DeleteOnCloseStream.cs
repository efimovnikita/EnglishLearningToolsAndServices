namespace TTSApi;

public class DeleteOnCloseStream : FileStream
{
    public DeleteOnCloseStream(string path, FileMode mode) : base(path, mode)
    {
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            File.Delete(Name);
        }
    }
}