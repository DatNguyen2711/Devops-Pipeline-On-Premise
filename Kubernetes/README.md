## Running Application On Kubernetes Cluster

### Tech using

*VMWare*

*MetalLB*

*Ingress Controller*

*NFS Server*

*Cri-o*

*Bitnami/Sealed Secret*

*Hashicorp Vault*


Step 1: First, config the ConnectionStrings at back-end

```bash
    "ConnectionStrings": {
      "MyDb": "Server=sqlserver.pharmacy-app.svc.cluster.local,1433; Database=MedicineWeb; User Id=sa; Password=DatLaid234555@Xy; MultipleActiveResultSets=true; TrustServerCertificate=True"
  },
```

And also edit the context file ProjectPrn231Context.cs

```bash

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=sqlserver.pharmacy-app.svc.cluster.local,1433; Database=MedicineWeb; User Id=sa; Password=DatLaid234555@Xy; MultipleActiveResultSets=true; TrustServerCertificate=True");


```

Step 2: Build image for back-end

```bash
cd Medicine-Web

docker build -t be:v1 .

docker tag be:v1 datnd2711/pharmacy-be:tagname

docker push datnd2711/pharmacy-be:tagname

```

Step 3: Build image for front-end

Config the .env with your back-end service in your cluster (basically we using NodePort service)


```bash
docker build -t fe:v1 .

docker tag fe:v1 datnd2711/pharmacy-fe:{tag_name}

docker push datnd2711/pharmacy-fe:{tag_name}
```

Step 4: Build image cho Database

```bash
cd Database

docker build -t db:{tag_name} -f Dockerfile.Database .

docker tag db:{tag_name} datnd2711/sqlserver:{tagname}

docker push  datnd2711/sqlserver:{tagname}

```
Step 5: Run Application on K8S cluster
```bash

kubectl create namespace pharmacy-app


git clone ...

cd {path_to_repo}/Kubernetes

kubectl apply -f .
```
## If you want to know how to set up NFS server, K8S-Cluster bare metal (Vmware) and MetalLB Nginx Ingress

### **[Access to my Notion](https://www.notion.so/datlaid/268200cd12cb4226834648f298512d44?v=cc87800a59da4616838c8f10919aebfb&pvs=4)**
