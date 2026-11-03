using System.Data.Common;

namespace InvoiSys.Application.Common.Coletas;

public sealed class ColetaPossePerdidaException(string message, Exception? innerException = null)
    : DbException(message, innerException);
