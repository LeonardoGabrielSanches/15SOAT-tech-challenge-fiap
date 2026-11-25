terraform {
  required_providers {
    random = {
      source = "hashicorp/random"
    }
  }
}

resource "random_password" "admin_keycloak" {
  length  = 24
  special = false
}

resource "random_password" "client_secret_web" {
  length  = 32
  special = false
}

resource "random_password" "auth_secret" {
  length  = 48
  special = false
}

resource "random_password" "mailpit" {
  length  = 24
  special = false
}
