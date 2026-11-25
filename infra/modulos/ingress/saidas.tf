output "dns_load_balancer" {
  value = data.kubernetes_service_v1.traefik.status[0].load_balancer[0].ingress[0].hostname
}
