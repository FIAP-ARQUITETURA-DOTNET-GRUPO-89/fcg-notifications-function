resource "aws_mq_broker" "rabbitmq" {
  broker_name = var.mq_broker_name

  engine_type         = "RabbitMQ"
  engine_version      = var.mq_engine_version
  host_instance_type  = var.mq_host_instance_type
  deployment_mode     = var.mq_deployment_mode
  storage_type        = "ebs"
  publicly_accessible = var.mq_publicly_accessible

  # SINGLE_INSTANCE usa apenas 1 subnet.
  subnet_ids = [data.aws_subnets.default.ids[0]]

  security_groups = [aws_security_group.rabbitmq_broker.id]

  # O Amazon MQ para RabbitMQ permite provisionar só este usuário administrador via API;
  # usuários/permissões adicionais (se quiser separar leitura/escrita por serviço) precisam
  # ser criados depois pelo console de gerenciamento do RabbitMQ.
  user {
    username = var.rabbitmq_admin_username
    password = var.rabbitmq_admin_password
  }

  tags = {
    Name = var.mq_broker_name
  }
}

# Credenciais do broker guardadas no Secrets Manager, no formato que o
# source_access_configuration (BASIC_AUTH) do event source mapping do Lambda espera.
resource "aws_secretsmanager_secret" "rabbitmq_credentials" {
  name        = "${var.project_name}/rabbitmq/broker-credentials"
  description = "Credenciais do broker Amazon MQ (RabbitMQ) usadas pelo event source mapping do Lambda ${var.lambda_function_name}"
}

resource "aws_secretsmanager_secret_version" "rabbitmq_credentials" {
  secret_id = aws_secretsmanager_secret.rabbitmq_credentials.id
  secret_string = jsonencode({
    username = var.rabbitmq_admin_username
    password = var.rabbitmq_admin_password
  })
}
