variable "regiao" {
  type    = string
  default = "us-east-1"
}

variable "nome_cluster" {
  type    = string
  default = "oficina"
}

variable "arn_usuario_desenvolvedor" {
  type = string
}

variable "nome_role_ci" {
  type    = string
  default = "oficina-ci"
}

variable "versao_kubernetes" {
  type    = string
  default = "1.34"
}
