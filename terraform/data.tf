# Execution role já existente (LabRole no AWS Academy) - ver variables.tf/existing_iam_role_name.
data "aws_iam_role" "lab_role" {
  name = var.existing_iam_role_name
}

# Usa a VPC default da conta/região para o broker Amazon MQ e o event source mapping do Lambda.
# Simplifica o setup para o trabalho; numa conta "de verdade" o ideal seria uma VPC dedicada.
data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}
