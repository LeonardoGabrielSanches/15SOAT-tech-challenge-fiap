variable "vpc_id" {
  type = string
}

variable "subnets_privadas" {
  type = list(string)
}

variable "security_group_nodes_id" {
  type = string
}

variable "nome_database" {
  type    = string
  default = "oficina"
}

variable "usuario_mestre" {
  type    = string
  default = "oficina"
}
