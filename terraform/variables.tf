variable "aws_region" {
  description = "Região AWS onde os recursos serão criados."
  type        = string
  default     = "us-east-1"
}

variable "aws_profile" {
  description = "Profile do ~/.aws/credentials a usar (opcional; no Learner Lab normalmente não é necessário, ver providers.tf)."
  type        = string
  default     = null
}

variable "project_name" {
  description = "Nome curto do projeto, usado como prefixo/tag nos recursos."
  type        = string
  default     = "fcg-notifications"
}

variable "environment" {
  description = "Nome do ambiente (ex.: dev, academy)."
  type        = string
  default     = "academy"
}

# ---------------------------------------------------------------------------
# IAM
# ---------------------------------------------------------------------------

variable "existing_iam_role_name" {
  description = <<-EOT
    Nome de uma IAM role JÁ EXISTENTE na conta, a ser usada como execution role do Lambda.
    No AWS Academy Learner Lab normalmente não é possível criar roles/policies novas
    (iam:CreateRole é bloqueado) - a role fixa disponibilizada chama-se "LabRole" e já
    tem as permissões necessárias liberadas. Se você confirmar que consegue criar roles
    IAM normalmente na sua conta, me avise: eu troco o data source em data.tf por uma
    role (aws_iam_role + aws_iam_role_policy_attachment) criada pelo próprio Terraform.
  EOT
  type    = string
  default = "LabRole"
}

# ---------------------------------------------------------------------------
# Amazon MQ (RabbitMQ)
# ---------------------------------------------------------------------------

variable "mq_broker_name" {
  description = "Nome do broker Amazon MQ."
  type        = string
  default     = "fcg-notifications-rabbitmq"
}

variable "mq_engine_version" {
  description = "Versão do engine RabbitMQ do broker. Confirme as versões disponíveis na sua região com `aws mq describe-broker-engine-types --engine-type RabbitMQ`."
  type        = string
  default     = "3.13"
}

variable "mq_host_instance_type" {
  description = "Classe de instância do broker. mq.t3.micro é a menor/mais barata, adequada para o Learner Lab."
  type        = string
  default     = "mq.t3.micro"
}

variable "mq_deployment_mode" {
  description = "SINGLE_INSTANCE (1 nó, mais barato) ou CLUSTER_MULTI_AZ. Para o trabalho, SINGLE_INSTANCE é suficiente."
  type        = string
  default     = "SINGLE_INSTANCE"
}

variable "rabbitmq_admin_username" {
  description = "Usuário administrador do broker RabbitMQ (único usuário que o Amazon MQ permite criar via API; usuários adicionais precisam ser criados depois pelo console/Management API do RabbitMQ)."
  type        = string
  default     = "fcgadmin"
}

variable "rabbitmq_admin_password" {
  description = "Senha do usuário administrador do broker (12-250 caracteres, pelo menos 4 caracteres únicos, sem vírgula). Defina via terraform.tfvars (não versionado) ou variável de ambiente TF_VAR_rabbitmq_admin_password."
  type        = string
  sensitive   = true
}

variable "mq_publicly_accessible" {
  description = "Se o broker deve ter endpoint público. Mantenha false: o Lambda acessa o broker pela mesma VPC/subnet, e os publishers (outros microsserviços) devem estar na mesma rede."
  type        = bool
  default     = false
}

variable "additional_ingress_cidr_blocks" {
  description = "CIDRs extras liberados no security group do broker (ex.: [\"SEU_IP/32\"]), úteis se publicly_accessible = true e você quiser acessar o console/AMQPS do seu computador."
  type        = list(string)
  default     = []
}

variable "payment_processed_queue_name" {
  description = "Nome da fila RabbitMQ consumida para eventos de pagamento processado."
  type        = string
  default     = "notifications-payment-processed-events"
}

variable "user_created_queue_name" {
  description = "Nome da fila RabbitMQ consumida para eventos de usuário criado."
  type        = string
  default     = "notifications-user-created-events"
}

variable "rabbitmq_virtual_host" {
  description = "Virtual host do RabbitMQ onde as filas vivem."
  type        = string
  default     = "/"
}

# ---------------------------------------------------------------------------
# Lambda (FcgNotifications.Function)
# ---------------------------------------------------------------------------

variable "lambda_function_name" {
  description = "Nome da função Lambda."
  type        = string
  default     = "fcg-notifications-function"
}

variable "lambda_zip_path" {
  description = <<-EOT
    Caminho local do pacote .zip de deploy, gerado com:
      cd src/FcgNotifications.Function
      dotnet lambda package -c Release -o ../../terraform/build/function.zip
    (requer a ferramenta global `dotnet tool install -g Amazon.Lambda.Tools`)
  EOT
  type    = string
  default = "./build/function.zip"
}

variable "lambda_handler" {
  description = "Handler no formato Assembly::Namespace.Classe::Metodo (mesmo valor do aws-lambda-tools-defaults.json)."
  type        = string
  default     = "FcgNotifications.Function::FcgNotifications.Function.Function::FunctionHandler"
}

variable "lambda_runtime" {
  description = "Runtime gerenciado do Lambda. Confirme no console/CLI se 'dotnet10' já está disponível na sua conta; caso não esteja, use 'provided.al2023' com um custom runtime bootstrap."
  type        = string
  default     = "dotnet10"
}

variable "lambda_memory_size" {
  description = "Memória (MB) da função, mesmo valor do aws-lambda-tools-defaults.json."
  type        = number
  default     = 512
}

variable "lambda_timeout" {
  description = "Timeout (segundos) da função."
  type        = number
  default     = 30
}

variable "lambda_batch_size" {
  description = "Quantidade máxima de mensagens entregues por invocação (event source mapping)."
  type        = number
  default     = 1
}

# ---------------------------------------------------------------------------
# Postgres (Amazon RDS)
# ---------------------------------------------------------------------------

variable "postgres_engine_version" {
  description = "Versão (major, ex.: \"17\") do PostgreSQL no RDS. Confirme a disponível na sua região com `aws rds describe-db-engine-versions --engine postgres --query \"DBEngineVersions[].EngineVersion\"`."
  type        = string
  default     = "17"
}

variable "postgres_instance_class" {
  description = "Classe de instância do RDS. db.t3.micro é a menor/mais barata, adequada para o Learner Lab."
  type        = string
  default     = "db.t3.micro"
}

variable "postgres_allocated_storage" {
  description = "Armazenamento (GB) do RDS."
  type        = number
  default     = 20
}

variable "postgres_master_username" {
  description = "Usuário master do RDS."
  type        = string
  default     = "fcgadmin"
}

variable "postgres_master_password" {
  description = "Senha do usuário master do RDS (8-128 caracteres). Defina via terraform.tfvars (não versionado) ou variável de ambiente TF_VAR_postgres_master_password."
  type        = string
  sensitive   = true
}

variable "postgres_app_database_name" {
  description = <<-EOT
    Nome do banco que a function usa (ConnectionStrings__Default). Não é criado pelo
    Terraform/RDS diretamente (o nome tem hífen, que o parâmetro db_name do RDS não
    aceita) - é criado sozinho na primeira vez que rodar `dotnet ef database update`
    contra o endpoint do RDS (o Npgsql/EF Core cria o banco se ele não existir).
  EOT
  type    = string
  default = "fcgnotifications-db"
}

variable "postgres_publicly_accessible" {
  description = <<-EOT
    Se o RDS deve ter endpoint público. true simplifica o setup no Learner Lab (o Lambda
    não precisa de vpc_config pra alcançar o banco, e você consegue rodar as migrations
    do seu computador) às custas de expor a porta 5432 na internet - mitigado por uma
    senha forte e, idealmente, por postgres_additional_ingress_cidr_blocks. false exigiria
    colocar o Lambda dentro da VPC (vpc_config) e normalmente um NAT Gateway pra ele
    continuar alcançando outros serviços da AWS fora da VPC - mais complexo pro Learner Lab.
  EOT
  type    = bool
  default = true
}

variable "postgres_additional_ingress_cidr_blocks" {
  description = <<-EOT
    CIDRs liberados na porta 5432 do RDS (ex.: ["SEU_IP/32"]). Se vazio E
    postgres_publicly_accessible = true, libera de qualquer lugar (0.0.0.0/0) - o Lambda
    (fora da VPC, sem vpc_config) usa IPs públicos dinâmicos da AWS, então não dá pra
    restringir só a ele. Preencha com o seu IP se quiser reduzir a exposição mesmo assim.
  EOT
  type    = list(string)
  default = []
}
