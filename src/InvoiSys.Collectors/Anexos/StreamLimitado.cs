using InvoiSys.Application.Common.Exceptions;

namespace InvoiSys.Collectors.Anexos;

/// <summary>
/// Repassa o conteúdo da origem sem guardá-lo em memória, interrompendo se
/// passar do tamanho máximo ou do prazo total. Ao ser descartado, libera a
/// resposta HTTP que originou o fluxo.
/// </summary>
internal sealed class StreamLimitado(
    Stream origem,
    IDisposable resposta,
    long limiteBytes,
    TimeSpan prazo) : Stream
{
    private readonly long _prazoFinal = Environment.TickCount64 + (long)prazo.TotalMilliseconds;
    private long _lidos;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        VerificarPrazo();
        return Registrar(origem.Read(buffer, offset, count));
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        VerificarPrazo();
        return Registrar(await origem.ReadAsync(buffer, cancellationToken));
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    private int Registrar(int lidos)
    {
        _lidos += lidos;
        if (_lidos > limiteBytes)
            throw new BusinessException("O anexo excede o tamanho máximo permitido.");

        return lidos;
    }

    private void VerificarPrazo()
    {
        if (Environment.TickCount64 > _prazoFinal)
            throw new BusinessException("O download do anexo excedeu o tempo limite.");
    }

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
