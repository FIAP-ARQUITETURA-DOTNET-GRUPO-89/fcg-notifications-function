namespace FcgNotifications.SharedKernel.Exceptions;

public sealed class AlreadyExistsException(string message) : BusinessException(message);
