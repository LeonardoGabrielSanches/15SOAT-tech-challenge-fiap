variable "nome_cluster" {
  type = string
}

variable "versao_chart_traefik" {
  type    = string
  default = "43.0.0"
}

variable "espera_remocao_load_balancer" {
  type    = string
  default = "120s"
}
