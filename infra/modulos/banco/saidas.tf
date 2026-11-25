output "endpoint" {
  value = aws_db_instance.banco.endpoint
}

output "senha" {
  value     = random_password.mestre.result
  sensitive = true
}

output "usuario" {
  value = aws_db_instance.banco.username
}
