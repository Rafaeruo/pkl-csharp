using System.Text;

namespace Pkl.Process1;


public class StdOutputReader : StdOutputReaderBase
{
    public StdOutputReader(StreamReader standardOutput)
    {
        this.StandardOutput = standardOutput;
    }

    public void Start()
    {
        this.stop = false;
        this.BeginReadAsync();
    }

    public void Stop()
    {
        this.OnReadStop();
    }
}

public abstract class StdOutputReaderBase
{
    // TODO define a definitve size for this buffe
    protected readonly byte[] buffer = new byte[256];

    protected volatile bool stop;

    protected StreamReader StandardOutput { get; set; } = default!;

    public event EventHandler<StdoutReadEventArgs>? StandardOutputRead;

    protected void BeginReadAsync()
    {
        if (!this.stop) 
        {
            this.StandardOutput.BaseStream.BeginRead(this.buffer, 0, this.buffer.Length, this.ReadHappened, null);
        }
    }

    protected virtual void OnReadStop()
    {
        this.stop = true;
        this.StandardOutput.DiscardBufferedData();
    }

    private void ReadHappened(IAsyncResult asyncResult)
    {
        var bytesRead = this.StandardOutput.BaseStream.EndRead(asyncResult);
        if (bytesRead == 0) 
        {
            this.OnReadStop();
            return;
        }

        var read = new ArraySegment<byte>(buffer, 0, bytesRead);

        this.OnInputRead(read);
        this.BeginReadAsync();
    }

    private void OnInputRead(ArraySegment<byte> input)
    {
        var handler = this.StandardOutputRead;
        if (handler == null) 
        {
            return;
        }

        handler(this, new StdoutReadEventArgs(input));
    }
}

public class StdoutReadEventArgs : EventArgs
{
    public StdoutReadEventArgs(ArraySegment<byte> stdout)
    {
        this.Stdout = stdout;
    }

    public ArraySegment<byte> Stdout { get; private set; }
}