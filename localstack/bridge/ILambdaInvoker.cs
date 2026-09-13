namespace FcgNotifications.Bridge;

public interface ILambdaInvoker
{
    /// <summary>
    /// Monta o mesmo formato de evento (RabbitMQEvent) que o Amazon MQ entregaria de
    /// verdade pro event source mapping do Lambda, e invoca a function no LocalStack.
    /// </summary>
    Task InvokeAsync<TMessage>(string queueName, TMessage message, CancellationToken cancellationToken);
}
