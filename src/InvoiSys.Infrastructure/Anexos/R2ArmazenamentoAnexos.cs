using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using InvoiSys.Application.Common.Anexos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiSys.Infrastructure.Anexos;

/// <summary>
/// Armazenamento dos anexos no Cloudflare R2, pela API compatível com S3.
/// Sem credenciais configuradas o serviço fica desligado e a aplicação segue
/// servindo os anexos direto do site de origem.
/// </summary>
public sealed class R2ArmazenamentoAnexos : IArmazenamentoAnexos, IDisposable
{
    private readonly AnexosOptions opcoes;
    private readonly ILogger<R2ArmazenamentoAnexos> logger;
    private readonly IAmazonS3? cliente;

    public R2ArmazenamentoAnexos(
        IOptions<AnexosOptions> opcoes,
        ILogger<R2ArmazenamentoAnexos> logger)
    {
        this.opcoes = opcoes.Value;
        this.logger = logger;

        if (!this.opcoes.Configurado)
        {
            logger.LogInformation(
                "Armazenamento de anexos desligado: credenciais do R2 não configuradas.");
            return;
        }

        cliente = new AmazonS3Client(
            new BasicAWSCredentials(this.opcoes.AccessKeyId, this.opcoes.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = this.opcoes.ServiceUrl,

                // O R2 não usa host por bucket e ignora a região; "auto" é o
                // valor que a própria Cloudflare indica.
                AuthenticationRegion = "auto",
                ForcePathStyle = true,

                // O R2 recusa o checksum que o SDK envia por padrão.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });
    }

    public bool Habilitado => cliente is not null;

    public async Task<bool> ExisteAsync(string chave, CancellationToken cancellationToken)
    {
        if (cliente is null)
            return false;

        try
        {
            await cliente.GetObjectMetadataAsync(opcoes.Bucket, chave, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception)
            when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task GravarAsync(
        string chave,
        Stream conteudo,
        string tipoConteudo,
        CancellationToken cancellationToken)
    {
        if (cliente is null)
            throw new InvalidOperationException("O armazenamento de anexos não está configurado.");

        var requisicao = new PutObjectRequest
        {
            BucketName = opcoes.Bucket,
            Key = chave,
            InputStream = conteudo,
            ContentType = tipoConteudo,

            // O fluxo vem de arquivo temporário, com tamanho conhecido: sem isso
            // o SDK usa chunked encoding, que o R2 não aceita.
            UseChunkEncoding = false,
            DisablePayloadSigning = true
        };

        await cliente.PutObjectAsync(requisicao, cancellationToken);
    }

    public async Task<Stream?> AbrirAsync(string chave, CancellationToken cancellationToken)
    {
        if (cliente is null)
            return null;

        try
        {
            var resposta = await cliente.GetObjectAsync(
                opcoes.Bucket, chave, cancellationToken);

            // O fluxo é devolvido para quem chamou, então a resposta só pode
            // ser descartada junto com ele; sem isso a conexão fica presa.
            return new StreamDaResposta(resposta.ResponseStream, resposta);
        }
        catch (AmazonS3Exception exception)
            when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // O registro no banco aponta para um objeto que não está mais lá.
            logger.LogWarning(
                "Anexo {Chave} registrado como arquivado, mas ausente no bucket.", chave);
            return null;
        }
    }

    public void Dispose() => cliente?.Dispose();
}
