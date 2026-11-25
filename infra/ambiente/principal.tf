data "aws_caller_identity" "atual" {}

module "rede" {
  source = "../modulos/rede"

  nome_cluster = var.nome_cluster
}

module "eks" {
  source = "../modulos/eks"

  nome_cluster              = var.nome_cluster
  versao_kubernetes         = var.versao_kubernetes
  subnets_publicas          = module.rede.subnets_publicas
  arn_usuario_desenvolvedor = var.arn_usuario_desenvolvedor
  arn_role_ci               = "arn:aws:iam::${data.aws_caller_identity.atual.account_id}:role/${var.nome_role_ci}"
}

module "banco" {
  source = "../modulos/banco"

  vpc_id                  = module.rede.vpc_id
  subnets_privadas        = module.rede.subnets_privadas
  security_group_nodes_id = module.eks.security_group_nodes_id
}

module "segredos" {
  source = "../modulos/segredos"
}

module "ingress" {
  source = "../modulos/ingress"

  nome_cluster = module.eks.nome_cluster
}
