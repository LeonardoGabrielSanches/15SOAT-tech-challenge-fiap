output "arn_role_ci" {
  value = aws_iam_role.ci.arn
}

output "nome_bucket_state" {
  value = aws_s3_bucket.state.id
}

output "urls_repositorios_ecr" {
  value = { for nome, repositorio in aws_ecr_repository.repositorio : nome => repositorio.repository_url }
}
