output "senha_admin_keycloak" {
  value     = random_password.admin_keycloak.result
  sensitive = true
}

output "client_secret_web" {
  value     = random_password.client_secret_web.result
  sensitive = true
}

output "auth_secret" {
  value     = random_password.auth_secret.result
  sensitive = true
}

output "senha_mailpit" {
  value     = random_password.mailpit.result
  sensitive = true
}
