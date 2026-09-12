"""
Cria (ou atualiza) a FcgNotifications.Function no LocalStack automaticamente, sem
precisar rodar 'awslocal lambda create-function' na mao toda vez.

Roda como um job "one-shot" no docker-compose, depois que o "package" ja gerou o
./build/function.zip: espera o LocalStack responder, e ai cria a function (se ainda
nao existir) ou atualiza o codigo + configuracao dela (se ja existir - por exemplo,
depois que voce mudou o handler e rodou 'docker compose up' de novo).
"""

import json
import os
import time

import boto3
from botocore.exceptions import ClientError

FUNCTION_NAME = os.environ["LAMBDA_FUNCTION_NAME"]
ZIP_PATH = os.environ["ZIP_PATH"]
HANDLER = os.environ["LAMBDA_HANDLER"]
RUNTIME = os.environ.get("LAMBDA_RUNTIME", "dotnet10")
ROLE_ARN = os.environ.get("LAMBDA_ROLE_ARN", "arn:aws:iam::000000000000:role/lambda-role")
TIMEOUT = int(os.environ.get("LAMBDA_TIMEOUT", "30"))
MEMORY_SIZE = int(os.environ.get("LAMBDA_MEMORY_SIZE", "512"))
ENV_JSON_PATH = os.environ.get("ENV_JSON_PATH")
LAMBDA_ENDPOINT_URL = os.environ["LAMBDA_ENDPOINT_URL"]


def log(message: str) -> None:
    print(f"[deploy] {message}", flush=True)


client = boto3.client(
    "lambda",
    endpoint_url=LAMBDA_ENDPOINT_URL,
    region_name=os.environ.get("AWS_DEFAULT_REGION", "us-east-1"),
)


def wait_localstack() -> None:
    while True:
        try:
            client.list_functions()
            log("LocalStack respondendo")
            return
        except Exception as exc:  # noqa: BLE001 - so queremos logar e tentar de novo
            log(f"aguardando LocalStack ficar pronto... ({exc})")
            time.sleep(3)


def read_env_variables() -> dict:
    if not ENV_JSON_PATH or not os.path.exists(ENV_JSON_PATH):
        return {}
    with open(ENV_JSON_PATH, encoding="utf-8") as f:
        return json.load(f).get("Variables", {})


def function_exists() -> bool:
    try:
        client.get_function(FunctionName=FUNCTION_NAME)
        return True
    except ClientError as exc:
        if exc.response.get("Error", {}).get("Code") == "ResourceNotFoundException":
            return False
        raise


def main() -> None:
    wait_localstack()

    if not os.path.exists(ZIP_PATH):
        raise SystemExit(f"zip não encontrado em '{ZIP_PATH}' - o serviço 'package' rodou com sucesso?")

    with open(ZIP_PATH, "rb") as f:
        zip_bytes = f.read()

    env_vars = read_env_variables()

    if function_exists():
        log(f"function '{FUNCTION_NAME}' já existe - atualizando código e configuração")
        client.update_function_code(FunctionName=FUNCTION_NAME, ZipFile=zip_bytes)
        client.get_waiter("function_updated_v2").wait(FunctionName=FUNCTION_NAME)
        client.update_function_configuration(
            FunctionName=FUNCTION_NAME,
            Handler=HANDLER,
            Timeout=TIMEOUT,
            MemorySize=MEMORY_SIZE,
            Environment={"Variables": env_vars},
        )
    else:
        log(f"criando function '{FUNCTION_NAME}'")
        client.create_function(
            FunctionName=FUNCTION_NAME,
            Runtime=RUNTIME,
            Role=ROLE_ARN,
            Handler=HANDLER,
            Code={"ZipFile": zip_bytes},
            Timeout=TIMEOUT,
            MemorySize=MEMORY_SIZE,
            Environment={"Variables": env_vars},
        )

    client.get_waiter("function_active_v2").wait(FunctionName=FUNCTION_NAME)
    log(f"function '{FUNCTION_NAME}' está Active")


if __name__ == "__main__":
    main()
