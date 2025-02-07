# Tech Stack

This project utilizes the following technologies:

| Technology       | Description                        |
|------------------|------------------------------------|
| <img src="https://upload.wikimedia.org/wikipedia/commons/7/7d/Microsoft_.NET_logo.svg" alt=".NET Core API 8" width="20" height="20"> **.NET Core API 8** | Backend                 |
| <img src="https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRcBiBI248rgjtRfFgxc8WapC-w7npSGPi6GnV1_VUMww&s" alt="ReactJS" width="20" height="20"> **ReactJS** | Frontend                |
| <img src="https://aptech.vn/wp-content/uploads/2021/04/sql-server.png" alt="SQL Server" width="20" height="20"> **SQL Server** | Database                         |
| <img src="https://techvccloud.mediacdn.vn/2018/7/13/docker-1531468887078532266614-0-14-400-726-crop-15314688919081778546108.png" alt="Docker" width="20" height="20"> **Docker** | Containerization                 |
| <img src="https://www.logicata.com/wp-content/uploads/2020/08/Amazon-EC2@4x-e1593195270371.png" alt="AWS EC2" width="20" height="20"> **AWS EC2** | Cloud infrastructure (server for Harbor)             |
| <img src="https://static-00.iconduck.com/assets.00/harbor-icon-1018x1024-15bgc40q.png" alt="Harbor Registry" width="20" height="20"> **Harbor Registry** | Container registry               |
| <img src="https://castrillo.gitlab.io/figaro/runner_logo.png" alt="Gitlab-CI" width="20" height="20"> **Gitlab-CI** | Continuous Integration           |
| <img src="https://upload.wikimedia.org/wikipedia/commons/thumb/e/e9/Jenkins_logo.svg/1200px-Jenkins_logo.svg.png" alt="Jenkins" width="20" height="20"> **Jenkins** | Continuous Integration           |
| <img src="https://manhha.dev/content/images/2022/03/00-featured-image-1.png" alt="Ubuntu" width="20" height="20"> **Ubuntu** | Operating System                 |
| <img src="https://encore.dev/assets/resources/kubernetes_cover.png" alt="Kubernetes" width="20" height="20"> **Kubernetes** | Container orchestration          |
| <img src="https://encore.dev/assets/resources/terraform_cover.png" alt="Terraform" width="20" height="20"> **Terraform** | Infrastructure as Code (IaC)     |
| <img src="https://upload.wikimedia.org/wikipedia/commons/thumb/3/36/Groovy-logo.svg/640px-Groovy-logo.svg.png" alt="Groovy" width="20" height="20"> **Groovy** | Scripting (Jenkins pipeline script)             |
| <img src="https://banner2.cleanpng.com/20180711/ysg/aawkk38yb.webp" alt="Helm" width="20" height="20"> **Helm** | Kubernetes package management    |
| <img src="https://tenten.vn/tin-tuc/wp-content/uploads/2023/09/tao-video-AI-bang-D-ID.jpg" alt="VMWare" width="20" height="20"> **VMWare** | Virtualization (100% infra using this)                   |
| <img src="https://cdn.prod.website-files.com/6203daf47137054c031fa0e6/63c7f4ab9471e63a01d7d285_argo-icon-color.png" alt="Argo CD" width="20" height="20"> **ArgoCD** | Gitops            |
| <img src="https://images.viblo.asia/6d8f3347-3bac-4d34-98a3-c25ab17ddb7c.png" alt="Argo Rollout" width="20" height="20"> **Argo-Rollout** | Blue/Green Deployment             |
| <img src="https://dyltqmyl993wv.cloudfront.net/assets/stacks/sealed-secrets/img/sealed-secrets-stack-220x234.png" alt="Bitnami Sealed Secrets" width="20" height="20"> **Bitnami Sealed Secrets** | Bitnami Sealed Secrets  (Secure Secret Management) |
| <img src="https://miro.medium.com/v2/resize:fit:1066/1*FAFtnHl7L7CcIRBzny3zMw.png" alt="Kaniko" width="20" height="20"> **Kaniko** | Build container images in Kubernetes without Docker daemon |
| <img src="https://www.netdata.cloud/img/vault.svg" alt="Vault" width="20" height="20"> **HashiCorp Vault**  | Secret management |
| <img src="https://blogger.googleusercontent.com/img/b/R29vZ2xl/AVvXsEiMVVYYgj_LY437FrV-KwNX9-rul33uYnRtgbk40J4GlNG29dkWyvnbtGgyBfrUqGW6e4hhbNO-WxGcOXNltbDyXDjpkCcugy0HcMxIYxBFCuQOKkMx8P2oIcL1D-hSYcZig4sGTkiFwG4/s1600/opentelemetry-stacked-color.png" alt="OpenTelemetry" width="20" height="20"> **OpenTelemetry** | Observability and Monitoring      |
| <img src="https://banner2.cleanpng.com/20190423/os/kisspng-prometheus-grafana-kubernetes-application-software-1713896726589.webp" alt="Prometheus" width="20" height="20"> **Prometheus** | Metrics collection and monitoring |
| <img src="https://www.skedler.com/blog/wp-content/uploads/2021/08/grafana-logo.png" alt="Grafana" width="20" height="20"> **Grafana** | Data visualization and monitoring |

# Getting Started

## First, run the development server:

```bash
docker compose up -d
```

Open [http://localhost:8900](http://localhost:8900) with your browser to see the result.

You can start editing the page by modifying `Medicine-Web-FE/src/component/auth/login`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) to write API and also ReactJS for client

## Learn More

## Deploy on EC2 Instance

The easiest way to deploy this app is to use the AWS EC2 [AWS Platform](https://ap-southeast-1.console.aws.amazon.com/ec2/home?region=ap-southeast-1#Home:)

Step 1: Register a EC2 (T2 medium above because SQL server need 2000MB to run) instance using Ubuntu ( Remember you open Inbound Rules of Instance before run the application [AWS Platform](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DAX.create-cluster.console.configure-inbound-rules.html) )

Step 2: Download Docker (lastest version is best)

```bash
mkdir -p /tools/docker

touch docker-install.sh && chmod +x docker-install.sh && vi docker-install.sh

#!/bin/bash
sudo apt install -y apt-transport-https ca-certificates curl software-properties-common
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update -y
sudo apt install docker-ce -y
sudo systemctl start docker
sudo systemctl enable docker
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
docker -v
docker-compose -v


./docker-install.sh


sudo usermod -aG docker $(whoami)

```

Step 3 : Clone this Repo and cd path/to/the repo

```bash
docker compose up -d
```

Step 4:
Go to the address:

```bash
http://{instance_public_IP_address}:8900
```

## More

You can using certbot and buy an domain

## Create EC2 Instance with Terraform

![image](https://github.com/DatNguyen2711/Pharmacy-Web/assets/81822483/b8fc4c6e-9102-43a1-8890-caffc5acf6ed)

Step 1: First, run you have to create AWS account and IAM user account

Open [IAM AWS docs](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_users.html) to see how to create AWS account and IAM user

Step 2: Attach to IAM user policies such as : AdministratorAccess (you can using only Administrator ec2 policies)

Click to the user then select security credentials and then follow the instructions to create access key for IAM
user (follow to the bellow instructions)
![image](https://github.com/DatNguyen2711/Pharmacy-Web/assets/81822483/cbeee82a-e05b-4449-ab5a-fd450cbb51f1)
then...
![image](https://github.com/DatNguyen2711/Pharmacy-Web/assets/81822483/e738ac15-12a3-406b-aad9-6cda8c7c84b9)
![image](https://github.com/DatNguyen2711/Pharmacy-Web/assets/81822483/fb448382-f8d7-413b-9f9f-8c0f110daa6d)
then name your tag, here is your access key
![image](https://github.com/DatNguyen2711/Pharmacy-Web/assets/81822483/9bf0abe7-6155-4b7a-9843-1d045ef6177b)

Step 3: Configure aws key in your local machine

```bash
aws configure


----
AWS Access Key ID [****************PAWK]: ************
AWS Secret Access Key [****************tz1d]: ***************
```

Step 4: Run this command to create and running EC2 Instance

```bash
terraform init

terraform plan

terraform apply --auto-approve

```
