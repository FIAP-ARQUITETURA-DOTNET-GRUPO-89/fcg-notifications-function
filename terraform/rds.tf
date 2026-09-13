resource "aws_db_subnet_group" "postgres" {
  name       = "${var.project_name}-postgres"
  subnet_ids = data.aws_subnets.default.ids

  tags = {
    Name = "${var.project_name}-postgres"
  }
}

resource "aws_security_group" "postgres" {
  name        = "${var.project_name}-postgres-sg"
  description = "Acesso ao Postgres (RDS) do ${var.project_name}"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description = "Postgres a partir da propria VPC (ex.: se algum dia colocar algo na VPC)"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [data.aws_vpc.default.cidr_block]
  }

  # Sem isso o Lambda (fora da VPC) e você (do seu computador, pra rodar as migrations)
  # não alcançam o RDS. Ver a descrição de var.postgres_publicly_accessible/
  # var.postgres_additional_ingress_cidr_blocks pro trade-off de segurança.
  dynamic "ingress" {
    for_each = var.postgres_publicly_accessible ? [1] : []
    content {
      description = "Postgres (RDS publico) - Lambda fora da VPC + acesso p/ rodar migrations"
      from_port   = 5432
      to_port     = 5432
      protocol    = "tcp"
      cidr_blocks = length(var.postgres_additional_ingress_cidr_blocks) > 0 ? var.postgres_additional_ingress_cidr_blocks : ["0.0.0.0/0"]
    }
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${var.project_name}-postgres-sg"
  }
}

resource "aws_db_instance" "postgres" {
  identifier = "${var.project_name}-postgres"

  engine         = "postgres"
  engine_version = var.postgres_engine_version
  instance_class = var.postgres_instance_class

  allocated_storage = var.postgres_allocated_storage
  storage_type      = "gp3"

  # Sem db_name: o banco "postgres" padrão do motor já serve de bootstrap. O banco de
  # verdade da function (var.postgres_app_database_name, com hífen) é criado sozinho na
  # primeira vez que rodar "dotnet ef database update" contra este endpoint.
  username = var.postgres_master_username
  password = var.postgres_master_password
  port     = 5432

  db_subnet_group_name   = aws_db_subnet_group.postgres.name
  vpc_security_group_ids = [aws_security_group.postgres.id]
  publicly_accessible    = var.postgres_publicly_accessible

  # Ambiente de estudo/Learner Lab - sem isso o "terraform destroy" trava pedindo snapshot.
  skip_final_snapshot = true
  deletion_protection = false
  apply_immediately   = true

  tags = {
    Name = "${var.project_name}-postgres"
  }
}
