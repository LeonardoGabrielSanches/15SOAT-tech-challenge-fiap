terraform {
  required_providers {
    helm = {
      source = "hashicorp/helm"
    }
    kubernetes = {
      source = "hashicorp/kubernetes"
    }
    time = {
      source = "hashicorp/time"
    }
  }
}

resource "time_sleep" "remocao_load_balancer" {
  destroy_duration = var.espera_remocao_load_balancer

  triggers = {
    cluster = var.nome_cluster
  }
}

resource "helm_release" "traefik" {
  name             = "traefik"
  namespace        = "traefik"
  create_namespace = true
  repository       = "https://traefik.github.io/charts"
  chart            = "traefik"
  version          = var.versao_chart_traefik

  wait    = true
  timeout = 600

  values = [yamlencode({
    ingressClass = {
      enabled        = true
      isDefaultClass = true
    }
    providers = {
      kubernetesCRD     = { enabled = true }
      kubernetesIngress = { enabled = true }
    }
  })]

  depends_on = [time_sleep.remocao_load_balancer]
}

data "kubernetes_service_v1" "traefik" {
  metadata {
    name      = "traefik"
    namespace = helm_release.traefik.namespace
  }
}
