output "nome_cluster" {
  value = aws_eks_cluster.principal.name

  depends_on = [
    aws_eks_access_policy_association.administrador,
    aws_eks_addon.padrao
  ]
}

output "endpoint" {
  value = aws_eks_cluster.principal.endpoint
}

output "certificado_autoridade" {
  value = aws_eks_cluster.principal.certificate_authority[0].data
}

output "security_group_nodes_id" {
  value = aws_eks_cluster.principal.vpc_config[0].cluster_security_group_id
}
