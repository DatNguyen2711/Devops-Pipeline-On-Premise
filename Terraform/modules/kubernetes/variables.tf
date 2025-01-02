variable "imnage_pull_secret" {
  type    = string
  default = "my-dockerhub-secret"
}

variable "namespace" {
  type = string
}


variable "deployment_config_backend" {
  description = "Configuration for the Kubernetes deployment"
  type = object({
    name           = string
    namespace      = string
    component      = string
    replicas       = number
    container_name = string
    image          = string
    container_port = number
    port_name      = string
    resources = object({
      requests = object({
        cpu    = string
        memory = string
      })
      limits = object({
        cpu    = string
        memory = string
      })
    })
    image_pull_secret_name = string
    docker_config_json     = string
  })
}
variable "deployment_config_frontend" {
  description = "Configuration for the Kubernetes deployment"
  type = object({
    name           = string
    namespace      = string
    component      = string
    replicas       = number
    container_name = string
    image          = string
    container_port = number
    port_name      = string
    resources = object({
      requests = object({
        cpu    = string
        memory = string
      })
      limits = object({
        cpu    = string
        memory = string
      })
    })
    liveness_probe = object({
      path                  = string
      port                  = string
      initial_delay_seconds = number
      period_seconds        = number
      failure_threshold     = number
    })
    readiness_probe = object({
      path                  = string
      port                  = string
      initial_delay_seconds = number
      period_seconds        = number
      failure_threshold     = number
    })
    image_pull_secret_name = string
    docker_config_json     = string
  })
}

variable "statefulset_config" {
  description = "Configuration for the SQL Server statefulset"
  type = object({
    name           = string
    namespace      = string
    component      = string
    container_name = string
    image          = string
    container_port = number
    volume_mounts = list(object({
      mount_path = string
      name       = string
    }))
    resources = object({
      requests = object({
        cpu    = string
        memory = string
      })
      limits = object({
        cpu    = string
        memory = string
      })
    })
    env = list(object({
      name  = string
      value = string
    }))
    secret_env = list(object({
      name        = string
      key         = string
      secret_name = string
    }))
    init_containers = list(object({
      name    = string
      image   = string
      command = list(string)
      args    = list(string)
      volume_mounts = list(object({
        mount_path = string
        name       = string
      }))
    }))
    image_pull_secret_name = string
    volume_claim_templates = list(object({
      name               = string
      access_modes       = list(string)
      storage_class_name = string
      requests = object({
        storage = string
      })
    }))
  })
}

variable "ingress_config" {
  description = "Configuration for the Ingress resource"
  type = object({
    name               = string
    namespace          = string
    annotations        = map(string)
    ingress_class_name = string
    tls = list(object({
      hosts       = list(string)
      secret_name = string
    }))
    rules = list(object({
      host = string
      paths = list(object({
        path      = string
        path_type = string
        backend = object({
          service_name = string
          service_port = string
        })
      }))
    }))
  })
}

variable "resource_quota" {
  type = object({
    spec = object({
      requests = object({
        cpu    = string
        memory = string
      })
      limits = object({
        cpu    = string
        memory = string
      })
    })
  })
}
