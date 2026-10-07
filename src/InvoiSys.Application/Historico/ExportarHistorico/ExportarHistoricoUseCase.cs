using System.Globalization;
using System.Text;
using FluentValidation;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Historico.ExportarHistorico;

public sealed class ExportarHistoricoUseCase(
    IValidator<ExportarHistoricoRequest> validator,
    IRegistroAuditoriaRepository registroAuditoriaRepository) : IExportarHistoricoUseCase
{
    public const int LimiteRegistros = 10_000;

    private const char Separador = ';';

    public async Task<ArquivoExportado> ExecutarAsync(
        ExportarHistoricoRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var registros = await registroAuditoriaRepository.ListarParaExportacaoAsync(
            request.ParaFiltro(),
            LimiteRegistros,
            cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine(string.Join(
            Separador,
            "Data e hora (UTC)",
            "Tipo",
            "Atividade",
            "Objeto",
            "Responsável",
            "Endereço IP"));

        foreach (var registro in registros)
            csv.AppendLine(FormatarLinha(registro));

        // BOM UTF-8 para o Excel reconhecer a acentuação ao abrir o arquivo.
        var conteudo = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(csv.ToString()))
            .ToArray();

        var nomeArquivo = $"historico-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";

        return new ArquivoExportado(conteudo, nomeArquivo, "text/csv");
    }

    public static string FormatarTipo(TipoEventoAuditoria tipo) => tipo switch
    {
        TipoEventoAuditoria.Coleta => "Coleta",
        TipoEventoAuditoria.Fonte => "Fonte",
        TipoEventoAuditoria.Autenticacao => "Autenticação",
        TipoEventoAuditoria.Usuario => "Usuário",
        _ => tipo.ToString()
    };

    private static string FormatarLinha(RegistroAuditoria registro) =>
        string.Join(
            Separador,
            registro.OcorridoEm.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Escapar(FormatarTipo(registro.Tipo)),
            Escapar(registro.Atividade),
            Escapar(registro.Objeto),
            Escapar(registro.Responsavel),
            Escapar(registro.EnderecoIp ?? string.Empty));

    private static string Escapar(string valor)
    {
        // Neutraliza fórmulas (CSV injection) ao abrir o arquivo em planilhas.
        if (valor.Length > 0 && "=+-@\t\r".Contains(valor[0]))
            valor = "'" + valor;

        if (valor.IndexOfAny([Separador, '"', '\n', '\r']) < 0)
            return valor;

        return $"\"{valor.Replace("\"", "\"\"")}\"";
    }
}
