# Infraestrutura - FcgNotifications.Function (Amazon MQ + Lambda)

Terraform que provisiona:

- Um broker **Amazon MQ for RabbitMQ** (`aws_mq_broker`), na VPC default, sem acesso público.
- Um banco **Postgres (Amazon RDS)** (`aws_db_instance`), com endpoint público (ver
  `postgres_publicly_accessible` em `variables.tf` pro trade-off) - simplifica o Lambda
  não precisar rodar dentro de uma VPC (que exigiria um NAT Gateway pra continuar
  alcançando o resto da AWS) e deixa você rodar as migrations do seu computador.
- A função **Lambda** `FcgNotifications.Function` (`aws_lambda_function`), usando a `LabRole` já existente na conta (AWS Academy Learner Lab não permite criar IAM roles novas).
- Dois **event source mappings** (`aws_lambda_event_source_mapping`), um para cada fila (`notifications-payment-processed-events` e `notifications-user-created-events`), que fazem o Lambda ser acionado nativamente pelas mensagens do RabbitMQ.
- Um **Secrets Manager secret** com as credenciais do broker, usado pelo `BASIC_AUTH` do event source mapping.

## Pré-requisitos

1. Terraform >= 1.7 instalado.
2. Credenciais AWS válidas exportadas no terminal (no AWS Academy Learner Lab: painel do lab → **AWS Details** → copiar `aws_access_key_id`, `aws_secret_access_key` e `aws_session_token` para variáveis de ambiente, ou colar no `~/.aws/credentials`). As credenciais do Learner Lab expiram em poucas horas - se o `terraform apply` começar a falhar com erro de autenticação, é isso.
3. Confirme que dá pra criar um broker Amazon MQ e uma instância RDS na sua conta (Console → Amazon MQ → Create broker → engine RabbitMQ; Console → RDS → Create database → engine PostgreSQL). Se der erro de permissão em algum dos dois, esse Terraform não vai funcionar como está - ver alternativa no fim deste README.
4. `dotnet tool install -g Amazon.Lambda.Tools` (se ainda não tiver).

## Passo a passo

### 1. Gerar o pacote de deploy do Lambda

A partir da raiz deste repositório (`fcg-notifications-function`):

```bash
cd src/FcgNotifications.Function
dotnet lambda package -c Release -o ../../terraform/build/function.zip
cd ../../terraform
```

### 2. Configurar as variáveis

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edite o `terraform.tfvars` com:
- `rabbitmq_admin_password`: uma senha forte (12-250 caracteres, ≥4 caracteres únicos, sem vírgula).
- `postgres_master_password`: uma senha forte (8-128 caracteres) pro RDS.
- Confirme `existing_iam_role_name` (padrão `"LabRole"`) - se sua conta permitir criar roles IAM normalmente, me avise que eu ajusto o `data.tf`/`lambda.tf` pra criar uma role dedicada em vez de reusar essa.

### 3. Provisionar

```bash
terraform init
terraform plan
terraform apply
```

O `aws_mq_broker` demora entre 15 e 30 minutos para ficar `RUNNING` na primeira criação - é normal o `apply` ficar parado nesse recurso (o RDS costuma ficar pronto bem mais rápido, uns 5-10 minutos).

### 4. Rodar as migrations no RDS

A função não roda migrations sozinha (isso é feito pelo `dotnet ef database update`, uma ferramenta de desenvolvimento, não algo pra rodar dentro do Lambda). Faça isso uma vez do seu computador, depois que o `apply` terminar:

```bash
terraform output postgres_endpoint
terraform output -raw postgres_connection_string
```

Com a connection string em mãos:

```bash
cd ../src/FcgNotifications.Infrastructure
ConnectionStrings__Default="<cole a connection string aqui>" dotnet ef database update
```

(no PowerShell: `$env:ConnectionStrings__Default = "<connection string>"` antes do `dotnet ef database update`). Isso cria o banco `fcgnotifications-db` no RDS (se ainda não existir) e aplica as migrations.

### 5. Criar as filas no RabbitMQ

O provider `aws` não tem um recurso para criar filas dentro de um broker RabbitMQ (isso é gerenciado pelo próprio RabbitMQ via AMQP/Management API, não pela API da AWS). Depois do `apply`:

1. Pegue a URL do console com `terraform output rabbitmq_console_url`.
2. Entre com o usuário/senha definidos em `rabbitmq_admin_username`/`rabbitmq_admin_password`.
3. Crie as duas filas com os nomes de `payment_processed_queue_name` e `user_created_queue_name` (padrão: `notifications-payment-processed-events` e `notifications-user-created-events`), no virtual host `/`.

Alternativa: se os serviços publishers (payments/users) já usam MassTransit com `ConfigureEndpoints`, as filas costumam ser criadas automaticamente na primeira publicação/consumo - nesse caso este passo manual pode não ser necessário, mas vale conferir no console se os nomes batem com o que o `Function.cs` espera.

### 6. Conferir se o Lambda foi realmente acionado

```bash
terraform output event_source_mapping_payment_processed_state
terraform output event_source_mapping_user_created_state
```

Deve aparecer `Enabled`. Publique uma mensagem de teste na fila (pelo console do RabbitMQ, aba da fila → "Publish message") e acompanhe os logs da função no CloudWatch Logs (`/aws/lambda/<lambda_function_name>`).

## Se a sua conta NÃO permitir Amazon MQ ou LabRole

Isso foi construído assumindo AWS Academy Learner Lab com a `LabRole` fixa e Amazon MQ liberado. Se durante o `terraform apply` aparecer erro de permissão negada em `mq:CreateBroker` ou `iam:GetRole`, avise antes de continuar mexendo - o desenho muda (teríamos que ir para SQS, ou para um bridge próprio consumindo o RabbitMQ e invocando o Lambda manualmente).

## Destruir os recursos

```bash
terraform destroy
```

Lembre de rodar isso quando terminar de testar, para não deixar o broker Amazon MQ e o RDS (ambos cobram por hora, mesmo nas classes `.t3.micro`) consumindo o crédito do Learner Lab sem necessidade.
