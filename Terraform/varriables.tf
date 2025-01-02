variable "kubeconfig_path" {
  description = "Path to the kubeconfig file"
  type        = string
  default     = "~/.kube/config"
}

variable "namespace" {
  description = "Kubernetes namespace for the application"
  default     = "pharmacy-app"
}
variable "deployment_config_frontend" {
  description = "Configuration for the frontend Kubernetes deployment"
  default = {
    name           = "front-end"
    namespace      = "pharmacy-app"
    component      = "front-end"
    replicas       = 2
    container_name = "front-end"
    image          = "datnd2711/pharmacy-fe:v5"
    container_port = 80
    port_name      = "http-frontend"
    resources = {
      requests = {
        cpu    = "100m"
        memory = "128Mi"
      }
      limits = {
        cpu    = "200m"
        memory = "256Mi"
      }
    }
    liveness_probe = {
      path                  = "/"
      port                  = "http-frontend"
      initial_delay_seconds = 30
      period_seconds        = 10
      failure_threshold     = 3
    }
    readiness_probe = {
      path                  = "/"
      port                  = "http-frontend"
      initial_delay_seconds = 15
      period_seconds        = 5
      failure_threshold     = 3
    }
    image_pull_secret_name = "my-dockerhub-secret"
  }
}
variable "deployment_config_backend" {
  description = "Configuration for the Kubernetes deployment"

  default = {
    name           = "back-end"
    namespace      = "pharmacy-app"
    component      = "back-end"
    replicas       = 3
    container_name = "back-end"
    image          = "datnd2711/pharmacy-be:v3"
    container_port = 8080
    port_name      = "http-backend"
    resources = {
      requests = {
        cpu    = "250m"
        memory = "256Mi"
      }
      limits = {
        cpu    = "500m"
        memory = "512Mi"
      }
    }
    image_pull_secret_name = "my-dockerhub-secret"
  }
}
variable "sqlserver_statefulset_config" {
  description = "Configuration for the SQL Server statefulset"
  default = {
    name           = "sqlserver"
    namespace      = "pharmacy-app"
    component      = "sqlserver"
    container_name = "sqlserver"
    image          = "datnd2711/sqlserver:v19"
    container_port = 1433
    volume_mounts = [{
      mount_path = "/var/opt/mssql/data"
      name       = "sqlserver-data"
    }]
    resources = {
      requests = {
        cpu    = "500m"
        memory = "1Gi"
      }
      limits = {
        cpu    = "1"
        memory = "2Gi"
      }
    }
    env = [{
      name  = "ACCEPT_EULA"
      value = "Y"
    }]
    secret_env = [{
      name        = "MSSQL_SA_PASSWORD"
      key         = "MSSQL_SA_PASSWORD"
      secret_name = "sqlserver-secret"
    }]
    init_containers = [{
      name    = "init-sqlserver"
      image   = "datnd2711/sqlserver:v19"
      command = ["/bin/bash", "-c"]
      args    = ["/dbconfig/attach.sh && /opt/mssql/bin/sqlservr && tail -f /dev/null"]
      volume_mounts = [{
        mount_path = "/var/opt/mssql/data"
        name       = "sqlserver-data"
      }]
    }]
    image_pull_secret_name = "my-dockerhub-secret"
    volume_claim_templates = [{
      name               = "sqlserver-data"
      access_modes       = ["ReadWriteOnce"]
      storage_class_name = "nfs-client"
      requests = {
        storage = "3Gi"
      }
    }]
  }
}

variable "ingress_config" {

  default = {
    name      = "pharmacy-ingress"
    namespace = "pharmacy-app"
    annotations = {
      "cert-manager.io/issuer" = "letsencrypt-prod"
    }
    ingress_class_name = "nginx"
    tls = [{
      hosts       = ["demo.datlaid.com"]
      secret_name = "pharmacy-app"
    }]
    rules = [{
      host = "demo.datlaid.com"
      paths = [{
        path      = "/"
        path_type = "Prefix"
        backend = {
          service_name = "front-end-service"
          service_port = "http-fe"
        }
        }, {
        path      = "/api"
        path_type = "Prefix"
        backend = {
          service_name = "back-end-service"
          service_port = "http-be"
        }
      }]
    }]
  }

}


variable "resource_quota" {
  type = object({
    spec = object({
      requests = object({
        cpu    = "1.5"
        memory = "2.5Gi"
      })
      limits = object({
        cpu    = "3"
        memory = "5Gi"
      })
    })
  })
}
