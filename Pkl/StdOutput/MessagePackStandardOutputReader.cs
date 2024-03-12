using System.Buffers;
using MessagePack;

namespace Pkl.StdOutput;

internal class MessagePackStandardOutputReader : IDisposable
{
    private readonly MessagePackStreamReader _reader;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _closed;

    public event EventHandler<StandardOutputReadEventArgs>? StandardOutputRead;

    public MessagePackStandardOutputReader(Stream standardOutput)
    {
        _reader = new MessagePackStreamReader(standardOutput);
    }

    public void Start()
    {
        _closed = false;
        BeginRead();
    }

    private void BeginRead()
    {   
        if (!_closed)
        {
            _reader.ReadAsync(_cancellationTokenSource.Token).AsTask().ContinueWith(ReadHappened);
        }
    }

    private void ReadHappened(Task<ReadOnlySequence<byte>?> read)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        if (read.Result is null)
        {
            return;
        }

        BeginRead();
        FireEvent(read.Result.Value);
    }

    private void FireEvent(ReadOnlySequence<byte> input)
    {
        var handler = StandardOutputRead;
        if (handler == null) 
        {
            return;
        }

        handler(this, new StandardOutputReadEventArgs(input));
    }

    public void Dispose()
    {
        _closed = true;
        _reader.Dispose();
        _cancellationTokenSource.Cancel();
    }
}

internal class StandardOutputReadEventArgs : EventArgs
{
    public StandardOutputReadEventArgs(ReadOnlySequence<byte> standardOutputData)
    {
        StandardOutputData = standardOutputData;
    }

    public ReadOnlySequence<byte> StandardOutputData { get; private set; }
}
