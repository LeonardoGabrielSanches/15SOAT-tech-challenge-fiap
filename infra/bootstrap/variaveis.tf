variable "regiao" {
  type    = string
  default = "us-east-1"
}

variable "nome_bucket_state" {
  type    = string
  default = "15soat-tech-challenge-fiap-leonardo-sanches"
}

variable "repositorio_github" {
  type    = string
  default = "LeonardoGabrielSanches/15SOAT-tech-challenge-fiap"
}

variable "branches_permitidas" {
  type    = list(string)
  default = ["main"]
}

variable "repositorios_ecr" {
  type    = list(string)
  default = ["oficina-api", "oficina-web"]
}

variable "imagens_mantidas" {
  type    = number
  default = 10
}
