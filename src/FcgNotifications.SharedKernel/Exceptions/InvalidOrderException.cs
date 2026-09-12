namespace FcgNotifications.SharedKernel.Exceptions;

public sealed class InvalidOrderException(string message): BusinessException(message);
