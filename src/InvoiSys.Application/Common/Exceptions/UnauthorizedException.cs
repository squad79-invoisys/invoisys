namespace InvoiSys.Application.Common.Exceptions;

public sealed class UnauthorizedException(string mensagem)
    : InvoiSysException(mensagem, 401);
