namespace InvoiSys.Domain.Exceptions;

public sealed class DomainException(string mensagem) : Exception(mensagem);
