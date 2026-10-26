namespace InvoiSys.Application.Coletas.ObterResumo;

public sealed record ResumoColetasResponse(
    int TotalExecucoes,
    int ExecucoesConcluidas,
    int ExecucoesComFalha,
    int ExecucoesEmAndamento);
