provider "aws" {
  region  = var.aws_region

  # No AWS Academy Learner Lab as credenciais normalmente vêm de variáveis de ambiente
  # (aws_access_key_id / aws_secret_access_key / aws_session_token, copiadas do painel
  # "AWS Details" do Learner Lab) em vez de um profile nomeado. Se preferir usar um
  # profile do ~/.aws/credentials, descomente a linha abaixo e ajuste var.aws_profile.
  # profile = var.aws_profile

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "terraform"
    }
  }
}
