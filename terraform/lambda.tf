resource "aws_lambda_function" "notifications_function" {
  function_name = var.lambda_function_name
  role          = data.aws_iam_role.lab_role.arn

  handler = var.lambda_handler
  runtime = var.lambda_runtime

  memory_size = var.lambda_memory_size
  timeout     = var.lambda_timeout

  filename         = var.lambda_zip_path
  source_code_hash = filebase64sha256(var.lambda_zip_path)

  environment {
    variables = {
      ConnectionStrings__Default = "Host=${aws_db_instance.postgres.address};Port=${aws_db_instance.postgres.port};Database=${var.postgres_app_database_name};Username=${var.postgres_master_username};Password=${var.postgres_master_password}"
    }
  }

  tags = {
    Name = var.lambda_function_name
  }
}

# Um event source mapping por fila: "queues" aceita exatamente 1 nome de fila por mapping.

resource "aws_lambda_event_source_mapping" "payment_processed" {
  event_source_arn = aws_mq_broker.rabbitmq.arn
  function_name    = aws_lambda_function.notifications_function.arn
  queues           = [var.payment_processed_queue_name]
  batch_size       = var.lambda_batch_size
  enabled          = true

  source_access_configuration {
    type = "BASIC_AUTH"
    uri  = aws_secretsmanager_secret_version.rabbitmq_credentials.arn
  }

  source_access_configuration {
    type = "VIRTUAL_HOST"
    uri  = var.rabbitmq_virtual_host
  }
}

resource "aws_lambda_event_source_mapping" "user_created" {
  event_source_arn = aws_mq_broker.rabbitmq.arn
  function_name    = aws_lambda_function.notifications_function.arn
  queues           = [var.user_created_queue_name]
  batch_size       = var.lambda_batch_size
  enabled          = true

  source_access_configuration {
    type = "BASIC_AUTH"
    uri  = aws_secretsmanager_secret_version.rabbitmq_credentials.arn
  }

  source_access_configuration {
    type = "VIRTUAL_HOST"
    uri  = var.rabbitmq_virtual_host
  }
}
