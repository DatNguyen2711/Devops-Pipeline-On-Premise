resource "kubernetes_namespace" "namespace" {
  metadata {
    name = var.namespace
  }
}
resource "kubernetes_secret" "dockerhub" {
  metadata {
    name      = var.imnage_pull_secret.name
    namespace = kubernetes_namespace.namespace.metadata[0].name
  }

  data = {
    ".dockerconfigjson" = base64encode(jsonencode({
      "auths" = {
        "https://index.docker.io/v1/" = {
          "username" = "datnd2711"
          "password" = "234555axy"
          "email"    = "datlaid@gmail.com"
          "auth"     = base64encode("datnd2711:234555axy")
        }
      }
    }))
  }

  type = "kubernetes.io/dockerconfigjson"
}
resource "kubernetes_secret" "sqlserver" {
  metadata {
    name      = "sqlserver-secret"
    namespace = kubernetes_namespace.namespace.metadata[0].name
  }

  data = {
    MSSQL_SA_PASSWORD = "RGF0TGFpZDIzNDU1NUBYeQ=="
  }

  type = "Opaque"
}
resource "kubernetes_deployment" "backend" {
  metadata {
    name      = var.deployment_config_backend.name
    namespace = kubernetes_namespace.namespace.metadata[0].name
    labels = {
      component = var.deployment_config_backend.component
    }
  }

  spec {
    replicas = var.deployment_config_backend.replicas

    selector {
      match_labels = {
        component = var.deployment_config_backend.component
      }
    }

    template {
      metadata {
        labels = {
          component = var.deployment_config_backend.component
        }
      }

      spec {
        container {
          name  = var.deployment_config_backend.container_name
          image = var.deployment_config_backend.image
          port {
            container_port = var.deployment_config_backend.container_port
            name           = var.deployment_config_backend.port_name
          }
          resources {
            requests = {
              cpu    = var.deployment_config_backend.resources.requests.cpu
              memory = var.deployment_config_backend.resources.requests.memory
            }
            limits = {
              cpu    = var.deployment_config_backend.resources.limits.cpu
              memory = var.deployment_config_backend.resources.limits.memory
            }
          }
        }

        image_pull_secrets {
          name = kubernetes_secret.dockerhub.metadata[0].name
        }
      }
    }
  }
}

resource "kubernetes_deployment" "frontend" {
  metadata {
    name      = var.deployment_config_frontend.name
    namespace = kubernetes_namespace.namespace.metadata[0].name
    labels = {
      component = var.deployment_config_frontend.component
    }
  }

  spec {
    replicas = var.deployment_config_frontend.replicas

    selector {
      match_labels = {
        component = var.deployment_config_frontend.component
      }
    }

    template {
      metadata {
        labels = {
          component = var.deployment_config_frontend.component
        }
      }

      spec {
        container {
          name  = var.deployment_config_frontend.container_name
          image = var.deployment_config_frontend.image
          port {
            container_port = var.deployment_config_frontend.container_port
            name           = var.deployment_config_frontend.port_name
          }
          resources {
            requests = {
              cpu    = var.deployment_config_frontend.resources.requests.cpu
              memory = var.deployment_config_frontend.resources.requests.memory
            }
            limits = {
              cpu    = var.deployment_config_frontend.resources.limits.cpu
              memory = var.deployment_config_frontend.resources.limits.memory
            }
          }
          liveness_probe {
            http_get {
              path = var.deployment_config_frontend.liveness_probe.path
              port = var.deployment_config_frontend.liveness_probe.port
            }
            initial_delay_seconds = var.deployment_config_frontend.liveness_probe.initial_delay_seconds
            period_seconds        = var.deployment_config_frontend.liveness_probe.period_seconds
            failure_threshold     = var.deployment_config_frontend.liveness_probe.failure_threshold
          }
          readiness_probe {
            http_get {
              path = var.deployment_config_frontend.readiness_probe.path
              port = var.deployment_config_frontend.readiness_probe.port
            }
            initial_delay_seconds = var.deployment_config_frontend.readiness_probe.initial_delay_seconds
            period_seconds        = var.deployment_config_frontend.readiness_probe.period_seconds
            failure_threshold     = var.deployment_config_frontend.readiness_probe.failure_threshold
          }
        }

        image_pull_secrets {
          name = kubernetes_secret.dockerhub.metadata[0].name
        }
      }
    }
  }
}

resource "kubernetes_stateful_set" "sqlserver" {
  metadata {
    name      = var.statefulset_config.name
    namespace = kubernetes_namespace.namespace.metadata[0].name
    labels = {
      component = var.statefulset_config.component
    }
  }

  spec {
    selector {
      match_labels = {
        component = var.statefulset_config.component
      }
    }

    service_name = "sqlserver"
    replicas     = 1

    template {
      metadata {
        labels = {
          component = var.statefulset_config.component
        }
      }

      spec {
        container {
          name  = var.statefulset_config.container_name
          image = var.statefulset_config.image

          port {
            container_port = var.statefulset_config.container_port
          }

          dynamic "volume_mounts" {
            for_each = var.statefulset_config.volume_mounts
            content {
              mount_path = volume_mounts.value.mount_path
              name       = volume_mounts.value.name
            }
          }

          resources {
            requests = {
              cpu    = var.statefulset_config.resources.requests.cpu
              memory = var.statefulset_config.resources.requests.memory
            }
            limits = {
              cpu    = var.statefulset_config.resources.limits.cpu
              memory = var.statefulset_config.resources.limits.memory
            }
          }

          dynamic "env" {
            for_each = var.statefulset_config.env
            content {
              name  = env.value.name
              value = env.value.value
            }
          }

          dynamic "env" {
            for_each = var.statefulset_config.secret_env
            content {
              name = env.value.name
              value_from {
                secret_key_ref {
                  name = env.value.secret_name
                  key  = env.value.key
                }
              }
            }
          }

          liveness_probe {
            exec {
              command = ["/bin/bash", "-c", "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $(cat /mnt/secrets-store/MSSQL_SA_PASSWORD) -Q 'SELECT 1'"]
            }
            initial_delay_seconds = 60
            period_seconds        = 20
            failure_threshold     = 6
          }

          readiness_probe {
            exec {
              command = ["/bin/bash", "-c", "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $(cat /mnt/secrets-store/MSSQL_SA_PASSWORD) -Q 'SELECT 1'"]
            }
            initial_delay_seconds = 60
            period_seconds        = 20
            failure_threshold     = 6
          }
        }

        dynamic "init_container" {
          for_each = var.statefulset_config.init_containers
          content {
            name    = init_container.value.name
            image   = init_container.value.image
            command = init_container.value.command
            args    = init_container.value.args

            dynamic "volume_mounts" {
              for_each = init_container.value.volume_mounts
              content {
                mount_path = volume_mounts.value.mount_path
                name       = volume_mounts.value.name
              }
            }
          }
        }

        image_pull_secrets {
          name = kubernetes_secret.dockerhub.metadata[0].name
        }
      }
    }

    dynamic "volume_claim_templates" {
      for_each = var.statefulset_config.volume_claim_templates
      content {
        metadata {
          name = volume_claim_templates.value.name
        }

        spec {
          access_modes       = volume_claim_templates.value.access_modes
          storage_class_name = volume_claim_templates.value.storage_class_name
          resources {
            requests = {
              storage = volume_claim_templates.value.requests.storage
            }
          }
        }
      }
    }
  }
}
resource "kubernetes_ingress" "ingress" {
  metadata {
    name        = var.ingress_config.name
    namespace   = kubernetes_namespace.namespace.metadata[0].name
    annotations = var.ingress_config.annotations
  }

  spec {
    ingress_class_name = var.ingress_config.ingress_class_name

    dynamic "tls" {
      for_each = var.ingress_config.tls
      content {
        hosts       = tls.value.hosts
        secret_name = tls.value.secret_name
      }
    }

    dynamic "rules" {
      for_each = var.ingress_config.rules
      content {
        host = rules.value.host

        http {
          dynamic "paths" {
            for_each = rules.value.paths
            content {
              path      = paths.value.path
              path_type = paths.value.path_type

              backend {
                service {
                  name = paths.value.backend.service_name
                  port {
                    name = paths.value.backend.service_port
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}



resource "kubernetes_resource_quota" "pharmacy_app_quota" {
  metadata {
    name      = "pharmacy-app-quota"
    namespace = "pharmacy-app"
  }

  spec {
    hard = {
      "requests.cpu"    = var.resource_quota.spec.requests.cpu
      "requests.memory" = var.resource_quota.spec.requests.memory
      "limits.cpu"      = var.resource_quota.spec.limits.cpu
      "limits.memory"   = var.resource_quota.spec.limits.memory
    }
  }
}

