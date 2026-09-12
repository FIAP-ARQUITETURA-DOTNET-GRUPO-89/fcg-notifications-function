output "lambda_function_name" {
  value = aws_lambda_function.notifications_function.function_name
}

output "lambda_function_arn" {
  value = aws_lambda_function.notifications_function.arn
}

output "rabbitmq_broker_id" {
  value = aws_mq_broker.rabbitmq.id
}

output "rabbitmq_broker_arn" {
  value = aws_mq_broker.rabbitmq.arn
}

output "rabbitmq_console_url" {
  description = "URL do console de gerenciamento do RabbitMQ (use para criar/inspecionar as filas manualmente, se necessário)."
  value       = try(aws_mq_broker.rabbitmq.instances[0].console_url, null)
}

output "rabbitmq_amqp_endpoint" {
  description = "Endpoint AMQPS do broker, para usar na connection string do MassTransit (amqps://...)."
  value       = try(aws_mq_broker.rabbitmq.instances[0].endpoints, null)
}

output "event_source_mapping_payment_processed_state" {
  value = aws_lambda_event_source_mapping.payment_processed.state
}

output "event_source_mapping_user_created_state" {
  value = aws_lambda_event_source_mapping.user_created.state
}

output "postgres_endpoint" {
  description = "Endpoint (host:port) do RDS. Use pra montar a connection string e rodar as migrations do seu computador."
  value       = aws_db_instance.postgres.endpoint
}

output "postgres_connection_string" {
  description = "Mesma connection string que a function usa (ConnectionStrings__Default) - útil pra rodar `dotnet ef database update` do seu computador."
  value       = "Host=${aws_db_instance.postgres.address};Port=${aws_db_instance.postgres.port};Database=${var.postgres_app_database_name};Username=${var.postgres_master_username};Password=${var.postgres_master_password}"
  sensitive   = true
}
