variable "nome_cluster" {
  type = string
}

variable "cidr_vpc" {
  type    = string
  default = "10.0.0.0/16"
}

variable "quantidade_azs" {
  type    = number
  default = 2
}
