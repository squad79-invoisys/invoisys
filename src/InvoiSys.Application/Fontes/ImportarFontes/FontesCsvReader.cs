using System.Text;
using InvoiSys.Application.Common.Exceptions;

namespace InvoiSys.Application.Fontes.ImportarFontes;

public sealed record RegistroCsv(int Linha, IReadOnlyList<string> Campos);

public sealed record ConteudoCsv(
    IReadOnlyList<string> Cabecalho,
    IReadOnlyList<RegistroCsv> Registros);

public static class FontesCsvReader
{
    public static ConteudoCsv Ler(string texto)
    {
        var separador = DetectarSeparador(texto);
        var registros = LerRegistros(texto, separador);

        if (registros.Count == 0)
            throw new BusinessException("O arquivo CSV está vazio.");

        var cabecalho = registros[0].Campos
            .Select(campo => campo.Trim())
            .ToList();

        return new ConteudoCsv(cabecalho, registros.Skip(1).ToList());
    }

    private static char DetectarSeparador(string texto)
    {
        var fim = texto.IndexOf('\n');
        var primeiraLinha = fim < 0 ? texto : texto[..fim];

        return primeiraLinha.Contains(';') && !primeiraLinha.Contains(',')
            ? ';'
            : ',';
    }

    private static List<RegistroCsv> LerRegistros(string texto, char separador)
    {
        var registros = new List<RegistroCsv>();
        var campos = new List<string>();
        var campo = new StringBuilder();
        var linhaFisica = 1;
        var linhaDoRegistro = 1;
        var entreAspas = false;
        var campoComAspas = false;

        void FecharCampo()
        {
            campos.Add(campo.ToString());
            campo.Clear();
            campoComAspas = false;
        }

        void FecharRegistro()
        {
            FecharCampo();

            var emBranco = campos.Count == 1 && string.IsNullOrWhiteSpace(campos[0]);
            if (!emBranco)
                registros.Add(new RegistroCsv(linhaDoRegistro, campos.ToList()));

            campos.Clear();
        }

        for (var i = 0; i < texto.Length; i++)
        {
            var c = texto[i];

            if (entreAspas)
            {
                if (c == '"')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '"')
                    {
                        campo.Append('"');
                        i++;
                    }
                    else
                    {
                        entreAspas = false;
                    }
                }
                else
                {
                    if (c == '\n')
                        linhaFisica++;

                    campo.Append(c);
                }

                continue;
            }

            if (c == '"' && campo.Length == 0 && !campoComAspas)
            {
                entreAspas = true;
                campoComAspas = true;
            }
            else if (c == separador)
            {
                FecharCampo();
            }
            else if (c == '\r' && i + 1 < texto.Length && texto[i + 1] == '\n')
            {
                // O '\n' seguinte fecha o registro.
            }
            else if (c == '\n')
            {
                FecharRegistro();
                linhaFisica++;
                linhaDoRegistro = linhaFisica;
            }
            else
            {
                campo.Append(c);
            }
        }

        if (entreAspas)
            throw new BusinessException(
                "O arquivo CSV tem um campo com aspas não fechadas.");

        if (campo.Length > 0 || campos.Count > 0 || campoComAspas)
            FecharRegistro();

        return registros;
    }
}
