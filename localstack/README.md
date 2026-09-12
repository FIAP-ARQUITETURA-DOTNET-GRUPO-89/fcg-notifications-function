# Rodando a FcgNotifications.Function no LocalStack

O LocalStack não simula o gatilho automático do Amazon MQ (RabbitMQ) — esse recurso
[não é implementado](https://github.com/localstack/localstack/issues/9645) (só têm
ActiveMQ, não RabbitMQ, nem no plano pago). Pra contornar isso só localmente, esse
compose sobe um **bridge** (`./bridge`): um script que fica ouvindo o RabbitMQ de
verdade e, a cada mensagem, invoca a Lambda no LocalStack sozinho — então "publica na
fila → a function roda" continua funcionando local, sem comando manual, mesmo o
LocalStack não suportando isso nativamente. O bridge não é parte do código da function
nem da IaC (isso é só o `../terraform`, que é o que a AWS de verdade usa via
`aws_lambda_event_source_mapping`) — é só uma ferramenta de teste/demonstração local.

## Pré-requisitos

1. **O `fcg-platform` (raiz do repo) rodando primeiro.** Esse compose aqui reaproveita
   o Postgres e o RabbitMQ que já sobem por lá (serviços `postgres`/`rabbitmq`, rede
   `fcg-network`) — não sobe os próprios. Então antes de qualquer coisa:

   ```powershell
   cd C:\source\Projetos Pessoais\fcg-platform
   docker compose -f docker-compose-development.yml up -d postgres rabbitmq
   ```

   (pode subir o compose inteiro se quiser as outras APIs rodando também, mas só
   `postgres`/`rabbitmq` já bastam pra este passo a passo).

2. Docker Desktop rodando (é só isso do resto — `dotnet-ef`, `Amazon.Lambda.Tools`,
   Python etc. **não** precisam estar instalados na sua máquina, o compose cuida disso
   em containers).
3. Conta gratuita no LocalStack (obrigatória desde 2026, mesmo pro tier "Hobby"):
   crie em [app.localstack.cloud](https://app.localstack.cloud), depois pegue seu token
   em **perfil → Auth Tokens**.
4. **AWS CLI v2** instalado - só é necessário pras seções 3 e 4 abaixo (invocar manual
   e ver logs); criar/atualizar a function em si já é automático (seção 1). Se não
   tiver o CLI:

   ```powershell
   winget install -e --id Amazon.AWSCLI
   ```

   Feche e abra o PowerShell de novo depois de instalar, e confirme com `aws --version`.

   > Não precisa instalar Python nem `awscli-local`/`awslocal` - a gente usa o `aws` CLI
   > oficial mesmo, só apontando ele pro LocalStack.

## 1. Subir tudo: migrations + empacotar + LocalStack + criar a function + bridge

```powershell
cd microservices\fcg-notifications-function\localstack
Copy-Item .env.example .env
notepad .env   # cole o LOCALSTACK_AUTH_TOKEN
docker compose up
```

Isso sobe, nessa ordem, tudo dentro de containers:

1. `migrate` - espera o `postgres` (da raiz do fcg-platform) responder, roda
   `dotnet ef database update` e sai (aparece como "exited (0)" - é esperado, é um job
   "one-shot").
2. `package` - roda `dotnet lambda package` e gera `./build/function.zip`, também sai
   depois.
3. `localstack` - sobe e fica escutando em `localhost:4566`.
4. `deploy` - espera o `package` terminar e o LocalStack responder, e cria a function
   sozinho (ou atualiza o código dela, se já existir) usando o `./build/function.zip` e
   o `./env.json` - substitui o `awslocal lambda create-function` manual. Também sai
   depois ("exited (0)").
5. `bridge` - fica rodando (não sai) esperando mensagens em
   `notifications-user-created-events` e `notifications-payment-processed-events` no
   RabbitMQ da raiz do fcg-platform, só depois que o `deploy` confirmar que a function
   está `Active`.

Na primeira vez demora um pouco mais (baixa as imagens do SDK do .NET/Python e restaura
os pacotes dentro dos containers `migrate`/`package`/`deploy`/`bridge`); das próximas
vezes fica bem mais rápido porque tudo fica em cache nos volumes
`dotnet-tools`/`nuget-cache`/`pip-cache`.

Se quiser rodar em background: `docker compose up -d`, e acompanhar com
`docker compose logs -f migrate package deploy bridge`.

Confirme que deu tudo certo:

```powershell
docker compose ps
```

`localstack` e `bridge` devem aparecer `running`; `migrate`, `package` e `deploy` devem
aparecer `exited (0)`. Se algum desses três sair com código diferente de 0, roda
`docker compose logs <nome-do-serviço>` pra ver o erro.

> Se aparecer erro `network fcg-network not found`, é porque o passo 1 dos
> pré-requisitos (subir a raiz do fcg-platform) não foi feito ainda.

> O `env.json` já está configurado com `ConnectionStrings__Default` apontando pro
> Postgres usando o nome do serviço (`postgres`) - isso funciona porque a Lambda que o
> LocalStack sobe entra na mesma rede Docker `fcg-network` (`LAMBDA_DOCKER_NETWORK` no
> `docker-compose.yml`). A role usada (`arn:aws:iam::000000000000:role/lambda-role`) é
> fake - o LocalStack não valida IAM de verdade, só precisa de um ARN com formato certo.

> O `PERSISTENCE=1` no `docker-compose.yml` salva a function em `./volume` (ignorado
> no `.gitignore`) entre reinicializações - o `deploy` mesmo assim roda toda vez que
> você dá `docker compose up`, então se você mudar o código e gerar um zip novo, ele
> atualiza a function sozinho também. Só perde tudo se apagar `./volume` ou rodar
> `docker compose down -v`.

## 2. (Opcional) Preparar o terminal pra usar o `awslocal`

Só precisa disso pras seções 3 (ver logs) e 4 (invocar manual) abaixo - criar/atualizar
a function já é automático (seção 1). O LocalStack não valida credenciais AWS de
verdade, mas o `aws` CLI exige que *alguma* esteja configurada. Roda isso uma vez em
cada PowerShell novo que você abrir (ou deixe permanente - ver dica no fim):

```powershell
$env:AWS_ACCESS_KEY_ID = "test"
$env:AWS_SECRET_ACCESS_KEY = "test"
$env:AWS_DEFAULT_REGION = "us-east-1"

function awslocal { aws --endpoint-url=http://localhost:4566 @Args }
```

Quer deixar isso permanente (sem repetir em toda janela nova)? Roda `notepad $PROFILE`
e cola o bloco acima lá dentro.

## 3. Testar o gatilho automático (via bridge)

Com `localstack` e `bridge` rodando e a function já `Active`, publique uma mensagem
de verdade na fila `notifications-user-created-events` do RabbitMQ e ela deve disparar
a function sozinha, sem nenhum comando de invoke manual — é o `bridge` fazendo esse
papel. Duas formas fáceis de publicar uma mensagem de teste:

**Opção A — painel do RabbitMQ** (mais simples pra um teste rápido): abra
`http://localhost:15672` (usuário/senha `guest`/`guest`), entre na fila
`notifications-user-created-events`, e em "Publish message" cole um payload JSON tipo:

```json
{"UserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "Name": "Maria Teste", "Email": "maria.teste@fcggames.com"}
```

**Opção B — de verdade**: suba a `users-api` da raiz do fcg-platform e crie um usuário
por ela (endpoint de cadastro) - ela publica o `UserCreatedEvent` real no RabbitMQ.

Depois, acompanhe:

```powershell
docker compose logs -f bridge
```

Você deve ver o bridge logando a mensagem recebida e a resposta da Lambda. Confira
também os logs da própria function:

```powershell
awslocal logs tail /aws/lambda/fcg-notifications-function --since 10m
```

> O `bridge` já cria a fila **e** o binding dela com o exchange que o MassTransit usa
> pra publicar (`QUEUE_EXCHANGE_MAP` no `docker-compose.yml`) toda vez que sobe - não
> precisa rodar a `users-api`/`payments-api` primeiro nem mexer em nada manual no
> painel do RabbitMQ. Isso importa porque o `rabbitmq` do compose da raiz não tem
> volume: toda vez que o container dele é recriado, esse binding se perde, e sem ele
> a mensagem publicada não chega em lugar nenhum (fica presa no exchange, sem erro
> nenhum aparecer). Se por algum motivo mudar o nome/namespace do evento no pacote
> `FgcGames.EventContracts`, atualiza o `QUEUE_EXCHANGE_MAP` (o nome certo aparece na
> aba **Exchanges** do painel do RabbitMQ, formato `<Namespace>:<TipoDoEvento>`).

## 4. Invocar manualmente (sem depender do bridge)

Também dá pra invocar a function direto, sem passar pelo RabbitMQ - útil pra testar
payloads específicos:

```powershell
$env:AWS_CLI_BINARY_FORMAT = "raw-in-base64-out"

awslocal lambda invoke `
  --function-name fcg-notifications-function `
  --payload file://events/user-created-event.json `
  response.json

Get-Content response.json
```

O evento em `events/user-created-event.json` já vem pronto: é um `UserCreatedEvent`
(usuário "Maria Teste") codificado em base64 dentro do formato `RabbitMQEvent` que o
Amazon MQ entregaria de verdade pro handler. Pra testar `PaymentProcessed`, preencha o
campo `data` de `events/payment-processed-event.json` com o base64 do seu
`PaymentProcessedEvent` - confira o enum `PaymentStatus` (em
`FgcGames.EventContracts.Enums`, via "Go to Definition" no Visual Studio) pra saber o
valor numérico certo do campo `Status`.

Pra gerar o base64 de um payload novo, no PowerShell:

```powershell
$json = '{"UserId":"...","Name":"...","Email":"..."}'
[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
```

## 5. Conferir o resultado

```powershell
awslocal logs tail /aws/lambda/fcg-notifications-function --since 10m
```

E confira no Postgres (`localhost:5432`, banco `fcgnotifications-db`) se apareceu a
linha nova nas tabelas `Users`/`Notifications`.

## Derrubar tudo

```powershell
docker compose down -v
```

(isso derruba só `migrate`/`package`/`localstack`/`deploy`/`bridge` - o `postgres`/
`rabbitmq` da raiz do fcg-platform continuam rodando, derrube por lá se quiser).
