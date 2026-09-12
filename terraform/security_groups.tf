resource "aws_security_group" "rabbitmq_broker" {
  name        = "${var.project_name}-rabbitmq-sg"
  description = "Acesso ao broker Amazon MQ (RabbitMQ) do ${var.project_name}"
  vpc_id      = data.aws_vpc.default.id

  # AMQPS (RabbitMQ criptografado - o Amazon MQ para RabbitMQ não expõe AMQP em texto puro).
  ingress {
    description = "AMQPS a partir de qualquer recurso dentro da própria VPC (Lambda, publishers, etc.)"
    from_port   = 5671
    to_port     = 5671
    protocol    = "tcp"
    cidr_blocks = [data.aws_vpc.default.cidr_block]
  }

  # Console de administração do RabbitMQ (HTTPS).
  ingress {
    description = "Console de gerenciamento RabbitMQ (HTTPS)"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = [data.aws_vpc.default.cidr_block]
  }

  # CIDRs extras (ex.: seu IP local) para você conseguir abrir o console de gerenciamento
  # do RabbitMQ e criar as filas manualmente, ou testar publish/consume do seu computador.
  # Preencha var.additional_ingress_cidr_blocks no terraform.tfvars se precisar.
  dynamic "ingress" {
    for_each = var.additional_ingress_cidr_blocks
    content {
      description = "Acesso adicional (AMQPS + console) - ${ingress.value}"
      from_port   = 443
      to_port     = 443
      protocol    = "tcp"
      cidr_blocks = [ingress.value]
    }
  }

  dynamic "ingress" {
    for_each = var.additional_ingress_cidr_blocks
    content {
      from_port   = 5671
      to_port     = 5671
      protocol    = "tcp"
      cidr_blocks = [ingress.value]
    }
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${var.project_name}-rabbitmq-sg"
  }
}
