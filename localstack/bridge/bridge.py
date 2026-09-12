"""
Bridge RabbitMQ -> Lambda (LocalStack).

O LocalStack nao emula o gatilho automatico do Amazon MQ (RabbitMQ) - so ActiveMQ e
suportado (https://github.com/localstack/localstack/issues/9645). Esse script existe so
pra simular localmente o comportamento que o aws_lambda_event_source_mapping (definido
em ../../terraform/lambda.tf) tem de verdade na AWS: fica consumindo as filas do
RabbitMQ e, a cada mensagem, monta o mesmo formato de evento (RabbitMQEvent) que o
Amazon MQ entregaria e invoca a function no LocalStack.

Nao faz parte do "codigo da function" nem da IaC - e so uma ferramenta de teste local.
"""

import base64
import json
import logging
import os
import time

import boto3
import pika

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [bridge] %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger("bridge")

RABBITMQ_URL = os.environ["RABBITMQ_URL"]
QUEUE_NAMES = [q.strip() for q in os.environ["QUEUE_NAMES"].split(",") if q.strip()]
LAMBDA_ENDPOINT_URL = os.environ["LAMBDA_ENDPOINT_URL"]
LAMBDA_FUNCTION_NAME = os.environ["LAMBDA_FUNCTION_NAME"]
AWS_REGION = os.environ.get("AWS_DEFAULT_REGION", "us-east-1")
VHOST = "/"

# O MassTransit publica cada evento num exchange "fanout" próprio, nomeado como
# "<Namespace>:<TipoDoEvento>" (ex.: "FgcGames.EventContracts.Events:UserCreatedEvent").
# Quem sempre criava a fila E o binding ligando ela a esse exchange era o Worker antigo
# (via ConfigureEndpoints do MassTransit) - como ele foi removido, e o "rabbitmq" do
# docker-compose da raiz não tem volume (perde tudo a cada down/up), o bridge recria
# esse binding sozinho toda vez que sobe, pra não depender de ninguém rodar antes dele
# nem de configurar nada manual no painel do RabbitMQ.
DEFAULT_QUEUE_EXCHANGE_MAP = {
    "notifications-user-created-events": "FgcGames.EventContracts.Events:UserCreatedEvent",
    "notifications-payment-processed-events": "FgcGames.EventContracts.Events:PaymentProcessedEvent",
}
QUEUE_EXCHANGE_MAP = json.loads(os.environ.get("QUEUE_EXCHANGE_MAP", "") or "null") or DEFAULT_QUEUE_EXCHANGE_MAP

lambda_client = boto3.client(
    "lambda",
    endpoint_url=LAMBDA_ENDPOINT_URL,
    region_name=AWS_REGION,
)


def connect_rabbitmq():
    while True:
        try:
            params = pika.URLParameters(RABBITMQ_URL)
            connection = pika.BlockingConnection(params)
            log.info("conectado ao RabbitMQ")
            return connection
        except Exception as exc:  # noqa: BLE001 - so queremos logar e tentar de novo
            log.warning("RabbitMQ ainda nao disponivel (%s); tentando de novo em 3s...", exc)
            time.sleep(3)


def build_event(queue_name: str, body: bytes, redelivered: bool) -> dict:
    return {
        "eventSource": "aws:rmq",
        "eventSourceArn": "arn:aws:mq:us-east-1:000000000000:broker:fcg-notifications-rabbitmq:local-bridge",
        "rmqMessagesByQueue": {
            f"{queue_name}::{VHOST}": [
                {
                    "redelivered": redelivered,
                    "data": base64.b64encode(body).decode("ascii"),
                }
            ]
        },
    }


def make_callback(queue_name: str):
    def on_message(channel, method, _properties, body):
        event = build_event(queue_name, body, method.redelivered)
        log.info("mensagem recebida em '%s' (%d bytes) - invocando %s", queue_name, len(body), LAMBDA_FUNCTION_NAME)

        try:
            response = lambda_client.invoke(
                FunctionName=LAMBDA_FUNCTION_NAME,
                InvocationType="RequestResponse",
                Payload=json.dumps(event).encode("utf-8"),
            )
        except Exception:
            log.exception(
                "falha ao chamar a Lambda no LocalStack - ela ja foi criada com "
                "'awslocal lambda create-function'? recolocando a mensagem na fila"
            )
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=True)
            time.sleep(3)
            return

        payload = response["Payload"].read().decode("utf-8", errors="replace")

        if response.get("FunctionError"):
            log.error("lambda retornou erro (%s): %s", response["FunctionError"], payload)
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=True)
            time.sleep(2)
            return

        log.info("lambda respondeu OK: %s", payload)
        channel.basic_ack(delivery_tag=method.delivery_tag)

    return on_message


def main():
    connection = connect_rabbitmq()
    channel = connection.channel()
    channel.basic_qos(prefetch_count=1)

    for queue_name in QUEUE_NAMES:
        # durable=True casa com o jeito que o MassTransit declarava essas filas no
        # Worker antigo.
        channel.queue_declare(queue=queue_name, durable=True)

        exchange_name = QUEUE_EXCHANGE_MAP.get(queue_name)
        if exchange_name:
            # fanout + durable=True e o que o MassTransit usa por padrao ao publicar -
            # declarar de novo aqui e idempotente (nao quebra nada se ja existir igual).
            channel.exchange_declare(exchange=exchange_name, exchange_type="fanout", durable=True)
            channel.queue_bind(queue=queue_name, exchange=exchange_name)
            log.info("fila '%s' vinculada ao exchange '%s'", queue_name, exchange_name)
        else:
            log.warning(
                "fila '%s' sem exchange conhecido em QUEUE_EXCHANGE_MAP - "
                "so vai receber mensagens publicadas direto nela (teste manual)",
                queue_name,
            )

        channel.basic_consume(queue=queue_name, on_message_callback=make_callback(queue_name))
        log.info("escutando fila: %s", queue_name)

    log.info("bridge pronto, aguardando mensagens (Ctrl+C pra parar)")
    try:
        channel.start_consuming()
    except KeyboardInterrupt:
        channel.stop_consuming()
        connection.close()


if __name__ == "__main__":
    main()
