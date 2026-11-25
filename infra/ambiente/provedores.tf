provider "aws" {
  region = var.regiao

  default_tags {
    tags = {
      Projeto = "oficina"
      Stack   = "ambiente"
    }
  }
}

locals {
  argumentos_token = ["eks", "get-token", "--cluster-name", module.eks.nome_cluster, "--region", var.regiao]
}

provider "kubernetes" {
  host                   = module.eks.endpoint
  cluster_ca_certificate = base64decode(module.eks.certificado_autoridade)

  exec {
    api_version = "client.authentication.k8s.io/v1beta1"
    command     = "aws"
    args        = local.argumentos_token
  }
}

provider "helm" {
  kubernetes = {
    host                   = module.eks.endpoint
    cluster_ca_certificate = base64decode(module.eks.certificado_autoridade)

    exec = {
      api_version = "client.authentication.k8s.io/v1beta1"
      command     = "aws"
      args        = local.argumentos_token
    }
  }
}
