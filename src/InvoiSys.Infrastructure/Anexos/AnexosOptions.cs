namespace InvoiSys.Infrastructure.Anexos;

public sealed class AnexosOptions
{
    public const string SectionName = "Anexos";

    /// <summary>Identificador da conta na Cloudflare; compõe o endereço do serviço.</summary>
    public string AccountId { get; init; } = string.Empty;

    public string Bucket { get; init; } = string.Empty;

    /// <summary>Vem de variável de ambiente ou user secrets, nunca do appsettings.</summary>
    public string AccessKeyId { get; init; } = string.Empty;

    /// <summary>Vem de variável de ambiente ou user secrets, nunca do appsettings.</summary>
    public string SecretAccessKey { get; init; } = string.Empty;

    /// <summary>
    /// Acima deste tamanho o arquivo não é copiado para o armazenamento
    /// próprio e continua sendo servido direto da origem.
    /// </summary>
    public long TamanhoMaximoBytes { get; init; } = 20L * 1024 * 1024;

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(AccountId) &&
        !string.IsNullOrWhiteSpace(Bucket) &&
        !string.IsNullOrWhiteSpace(AccessKeyId) &&
        !string.IsNullOrWhiteSpace(SecretAccessKey);

    public string ServiceUrl => $"https://{AccountId}.r2.cloudflarestorage.com";
}
