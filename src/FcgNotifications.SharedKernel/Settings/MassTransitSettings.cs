using System.ComponentModel.DataAnnotations;

namespace FcgNotifications.SharedKernel.Settings;

/// <summary>
/// Configuração global de retry do MassTransit utilizando estratégia exponencial.
///
/// Exemplo de linha do tempo (mensagem com falha contínua):
/// - T0: consumo inicial
/// - T+1s: retry 1
/// - T+3s: retry 2
/// - T+7s: retry 3
/// - T+15s: retry 4
/// - T+30s: retry 5 (limite atingido)
/// - Depois disso: mensagem é enviada para a fila de erro (DLQ/_error queue)
/// </summary>
public record MassTransitSettings
{
    /// <summary>
    /// Número máximo de tentativas de retry antes de a mensagem ser considerada falha.
    /// Após esse limite, a mensagem segue para erro (ou DLQ, se configurada).
    /// </summary>
    [Range(0, 20)]
    public int RetryLimit { get; init; } = 5;

    /// <summary>
    /// Intervalo mínimo (em segundos) entre a primeira e a segunda tentativa de retry.
    /// Usado como base no cálculo do backoff exponencial.
    /// </summary>
    [Range(1, 3600)]
    public int MinIntervalSeconds { get; init; } = 1;

    /// <summary>
    /// Intervalo máximo (em segundos) permitido entre tentativas de retry.
    /// Evita que o backoff exponencial cresça indefinidamente.
    /// </summary>
    [Range(1, 3600)]
    public int MaxIntervalSeconds { get; init; } = 30;

    /// <summary>
    /// Fator de variação aplicado ao crescimento do intervalo entre retries.
    /// Controla o “salto” entre tentativas no backoff exponencial.
    /// </summary>
    [Range(0, 3600)]
    public int IntervalDeltaSeconds { get; init; } = 2;
}
