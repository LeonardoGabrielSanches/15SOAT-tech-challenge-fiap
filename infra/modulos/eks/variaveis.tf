variable "nome_cluster" {
  type = string
}

variable "versao_kubernetes" {
  type = string
}

variable "subnets_publicas" {
  type = list(string)
}

variable "arn_usuario_desenvolvedor" {
  type = string
}

variable "arn_role_ci" {
  type = string
}

variable "tipo_instancia" {
  type    = string
  default = "c7i-flex.large"
}

variable "nodes_minimo" {
  type    = number
  default = 2
}

variable "nodes_desejado" {
  type    = number
  default = 2
}

variable "nodes_maximo" {
  type    = number
  default = 3
}
