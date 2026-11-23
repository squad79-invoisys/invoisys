namespace InvoiSys.Infrastructure.Anexos;

/// <summary>
/// Repassa o fluxo de uma resposta HTTP e descarta a resposta junto com ele.
/// Sem isso a conexão com o armazenamento só seria liberada pelo coletor de lixo.
/// </summary>
internal sealed class StreamDaResposta(Stream origem, IDisposable resposta) : Stream
{
    public override bool CanRead => origem.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        origem.Read(buffer, offset, count);

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default) =>
        origem.ReadAsync(buffer, cancellationToken);

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) =>
        origem.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            origem.Dispose();
            resposta.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await origem.DisposeAsync();
        resposta.Dispose();
        await base.DisposeAsync();
    }
}
