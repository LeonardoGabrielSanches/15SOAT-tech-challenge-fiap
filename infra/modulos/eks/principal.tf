terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
    }
  }
}

data "aws_iam_policy_document" "confianca_cluster" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["eks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "cluster" {
  name               = "oficina-eks-cluster"
  assume_role_policy = data.aws_iam_policy_document.confianca_cluster.json
}

resource "aws_iam_role_policy_attachment" "cluster" {
  role       = aws_iam_role.cluster.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSClusterPolicy"
}

resource "aws_eks_cluster" "principal" {
  name     = var.nome_cluster
  version  = var.versao_kubernetes
  role_arn = aws_iam_role.cluster.arn

  vpc_config {
    subnet_ids              = var.subnets_publicas
    endpoint_public_access  = true
    endpoint_private_access = false
  }

  access_config {
    authentication_mode                         = "API"
    bootstrap_cluster_creator_admin_permissions = false
  }

  depends_on = [aws_iam_role_policy_attachment.cluster]
}

locals {
  administradores = {
    desenvolvedor = var.arn_usuario_desenvolvedor
    ci            = var.arn_role_ci
  }
}

resource "aws_eks_access_entry" "administrador" {
  for_each = local.administradores

  cluster_name  = aws_eks_cluster.principal.name
  principal_arn = each.value
  type          = "STANDARD"
}

resource "aws_eks_access_policy_association" "administrador" {
  for_each = local.administradores

  cluster_name  = aws_eks_cluster.principal.name
  principal_arn = each.value
  policy_arn    = "arn:aws:eks::aws:cluster-access-policy/AmazonEKSClusterAdminPolicy"

  access_scope {
    type = "cluster"
  }

  depends_on = [aws_eks_access_entry.administrador]
}

data "aws_iam_policy_document" "confianca_nodes" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "nodes" {
  name               = "oficina-eks-nodes"
  assume_role_policy = data.aws_iam_policy_document.confianca_nodes.json
}

resource "aws_iam_role_policy_attachment" "nodes" {
  for_each = toset([
    "arn:aws:iam::aws:policy/AmazonEKSWorkerNodePolicy",
    "arn:aws:iam::aws:policy/AmazonEKS_CNI_Policy",
    "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
  ])

  role       = aws_iam_role.nodes.name
  policy_arn = each.value
}

resource "aws_eks_node_group" "principal" {
  cluster_name    = aws_eks_cluster.principal.name
  node_group_name = "principal"
  node_role_arn   = aws_iam_role.nodes.arn
  subnet_ids      = var.subnets_publicas
  instance_types  = [var.tipo_instancia]
  ami_type        = "AL2023_x86_64_STANDARD"

  scaling_config {
    min_size     = var.nodes_minimo
    desired_size = var.nodes_desejado
    max_size     = var.nodes_maximo
  }

  update_config {
    max_unavailable = 1
  }

  depends_on = [aws_iam_role_policy_attachment.nodes]
}

resource "aws_eks_addon" "padrao" {
  for_each = toset(["vpc-cni", "kube-proxy", "coredns", "metrics-server"])

  cluster_name                = aws_eks_cluster.principal.name
  addon_name                  = each.value
  resolve_conflicts_on_create = "OVERWRITE"
  resolve_conflicts_on_update = "OVERWRITE"

  depends_on = [aws_eks_node_group.principal]
}
