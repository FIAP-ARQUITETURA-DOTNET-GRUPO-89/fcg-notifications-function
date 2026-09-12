namespace FcgNotifications.SharedKernel.Exceptions;

public sealed class OrderLockedException(string message) : BusinessException(message);
