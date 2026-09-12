using FcgNotifications.Domain.Enums;
using FcgUsers.Domain.ValueObjects;

namespace FcgNotifications.Domain.Entities;

public class Notification : BaseEntity
{
    protected Notification() { }

    public Notification(Guid userId, Email userEmail, string message, NotificationType type)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("O ID do usuário é obrigatório.", nameof(userId));
        }

        if (userEmail is null)
        {
            throw new ArgumentNullException(nameof(userEmail), "O endereço de e-mail é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A mensagem da notificação não pode ser vazia.", nameof(message));
        }

        UserId = userId;
        UserEmail = userEmail;
        Message = message.Trim();
        Type = type;

        Status = NotificationStatus.Pending;
    }

    /// <summary>
    /// ID do usuário destinatário.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// E-mail do destinatário (Value Object).
    /// </summary>
    public Email UserEmail { get; private set; } = null!;

    /// <summary>
    /// Conteúdo da notificação.
    /// </summary>
    public string Message { get; private set; } = null!;

    /// <summary>
    /// Tipo de notificação (Boas-vindas ou Confirmação de Compra).
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// Status atual da notificação.
    /// </summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>
    /// Marca a notificação como enviada com sucesso.
    /// </summary>
    public void MarkAsSent()
    {
        EnsureStatus(NotificationStatus.Pending);

        Status = NotificationStatus.Sent;
        MarkAsUpdated();
    }

    /// <summary>
    /// Marca a notificação como falha no envio.
    /// </summary>
    public void MarkAsFailed()
    {
        Status = NotificationStatus.Failed;
        MarkAsUpdated();
    }

    private void EnsureStatus(NotificationStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException($"A notificação deve estar com status '{expectedStatus}' para esta operação. Status atual: {Status}");
        }
    }
}
