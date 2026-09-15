namespace InvoiSys.Application.Common.Exceptions;

public sealed class NotFoundException(string mensagem)
    : InvoiSysException(mensagem, 404);
