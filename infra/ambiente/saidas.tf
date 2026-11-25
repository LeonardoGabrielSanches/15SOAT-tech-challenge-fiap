output "nome_cluster" {
  value = module.eks.nome_cluster
}

output "dns_load_balancer" {
  value = module.ingress.dns_load_balancer
}

output "endpoint_rds" {
  value = module.banco.endpoint
}

output "senha_rds" {
  value     = module.banco.senha
  sensitive = true
}

output "senha_admin_keycloak" {
  value     = module.segredos.senha_admin_keycloak
  sensitive = true
}

output "client_secret_web" {
  value     = module.segredos.client_secret_web
  sensitive = true
}

output "auth_secret" {
  value     = module.segredos.auth_secret
  sensitive = true
}

output "senha_mailpit" {
  value     = module.segredos.senha_mailpit
  sensitive = true
}

output "usuario_rds" {
  value = module.banco.usuario
}
