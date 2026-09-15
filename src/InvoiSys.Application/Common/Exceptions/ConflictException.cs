namespace InvoiSys.Application.Common.Exceptions;

public sealed class ConflictException(string mensagem)
    : InvoiSysException(mensagem, 409);
