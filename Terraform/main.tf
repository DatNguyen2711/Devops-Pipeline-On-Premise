module "k8s" {
  source                     = "./modules/kubernetes"
  namespace                  = var.namespace
  statefulset_config         = var.sqlserver_statefulset_config
  deployment_config_backend  = var.deployment_config_backend
  deployment_config_frontend = var.deployment_config_frontend
  ingress_config             = var.ingress_config
  resource_quota             = var.resource_quota
}

