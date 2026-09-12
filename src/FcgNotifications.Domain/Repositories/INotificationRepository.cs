using FcgNotifications.Domain.Entities;

namespace FcgNotifications.Domain.Repositories;

public interface INotificationRepository
{
    /// <summary>
    /// Adiciona uma nova notificação ao contexto.
    /// </summary>
    /// <param name="notification">Notificação a ser adicionada.</param>
    void Add(Notification notification);

    /// <summary>
    /// Recupera uma notificação específica através do seu identificador único.
    /// </summary>
    /// <param name="id">O identificador único da notificação.</param>
    /// <param name="cancellationToken">Token utilizado para cancelar a consulta.</param>
    /// <returns>A instância da notificação se encontrado; caso contrário, nulo.</returns>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera uma lista de notificações pendentes para processamento.
    /// </summary>
    /// <param name="cancellationToken">Token utilizado para cancelar a consulta.</param>
    /// <returns>Uma lista de notificações com status Pending.</returns>
    Task<IReadOnlyCollection<Notification>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste no banco de dados todas as alterações pendentes no contexto atual.
    /// </summary>
    /// <returns>Quantidade de registros afetados.</returns>
    Task<int> SaveChangesAsync();
}
