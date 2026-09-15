namespace InvoiSys.Application.Common.Exceptions;

public sealed class BusinessException(string mensagem)
    : InvoiSysException(mensagem, 400);
