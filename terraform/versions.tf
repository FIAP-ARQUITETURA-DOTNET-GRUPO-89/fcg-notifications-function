terraform {
  required_version = ">= 1.7.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      # >= 5 já não bastava pro runtime "dotnet10" (adicionado à AWS Lambda em jan/2026,
      # mas a validação client-side do provider só reconhece o que a versão dele já
      # conhece) - precisa de uma versão recente da série 6.x.
      version = "~> 6.0"
    }
  }
}
