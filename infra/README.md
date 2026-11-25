# Infraestrutura na AWS

Tudo o que sobe a Oficina na AWS fica aqui: Terraform (duas stacks e cinco módulos), manifestos Kustomize e o roteiro de carga k6. O ambiente nasce por um push na `main`, vive no EKS e é destruído toda madrugada para economizar créditos. A justificativa das escolhas está no [ADR 0011](../docs/adr/0011-infraestrutura-na-aws-com-eks-sob-demanda.md); migrations e carga de demonstração rodam num Job, conforme o [ADR 0012](../docs/adr/0012-migrations-e-carga-de-demonstracao-em-job-de-uso-unico.md).

```
infra/
├── bootstrap/   stack permanente: bucket do state, OIDC do GitHub, role do CI, ECR
├── ambiente/    stack sob demanda: compõe os módulos abaixo
├── modulos/     rede, eks, banco, ingress e segredos
├── k8s/         manifestos Kustomize (API, web, Keycloak, Mailpit, Ingress, HPA)
│   └── migracao/  Job de migrations e carga de demonstração
└── carga/       roteiro k6 do teste de carga
```

## Recursos criados

### Stack `bootstrap` (permanente, aplicada uma vez pelo desenvolvedor)

| Recurso | Arquivo |
|---|---|
| Bucket S3 do state, com versionamento, criptografia SSE e bloqueio de acesso público | `bucket.tf` |
| Provedor OIDC do GitHub | `oidc.tf` |
| Role `oficina-ci` (trust restrita à `main`) e sua policy, com IAM limitado ao prefixo `oficina-` | `oidc.tf` |
| Repositórios ECR `oficina-api` e `oficina-web`, com scan on push e lifecycle de 10 imagens | `ecr.tf` |

Saídas: ARN da role do CI e URLs dos repositórios.

### Stack `ambiente` (sob demanda, destruída toda noite)

State em chave própria no mesmo bucket. O root só compõe os módulos.

| Módulo | Recursos AWS |
|---|---|
| `rede` | VPC, Internet Gateway, 2 subnets públicas (nodes), 2 subnets privadas (só RDS), tabelas de rota e associações. Sem NAT Gateway |
| `eks` | Cluster EKS 1.37 e sua role IAM, managed node group x86 `c7i-flex.large` (mín. 2, desejado 2, máx. 3) e sua role IAM, add-ons gerenciados (incluindo metrics-server, usado pelo HPA), access entries para o usuário do desenvolvedor e para a role do CI |
| `banco` | RDS PostgreSQL 17 `db.t4g.micro`, single-AZ, 20 GB, sem acesso público, subnet group e security group que aceita 5432 só dos nodes. Senha mestre gerada. Sem snapshot final nem proteção contra deleção |
| `ingress` | Traefik via `helm_release` com Service `LoadBalancer`: um único load balancer da AWS |
| `segredos` | Senhas geradas por `random_password`: admin do Keycloak, client secret da web, `AUTH_SECRET` e basic auth do Mailpit (não cria recurso na AWS; só existem no state) |

### No cluster (namespace `oficina`)

API (HPA de 2 a 5 réplicas), web (HPA de 1 a 3), Keycloak (banco `keycloak` no RDS, criado por um init container), Mailpit, Ingress do Traefik e o Job de migration.

## Pré-requisitos

- Conta AWS e usuário IAM do desenvolvedor, com o AWS CLI logado (`aws sts get-caller-identity`).
- Terraform 1.10 ou superior (o lock do state usa `use_lockfile`, sem DynamoDB).
- `kubectl` e `gh` (autenticado no fork ou repositório).
- Opcional: `k6`, para rodar a carga localmente.

O prefixo do `sub` do OIDC e o nome do bucket (`15soat-tech-challenge-fiap-leonardo-sanches`) são valores padrão em `bootstrap/variaveis.tf`. O GitHub usa o subject imutável deste repositório (`repo:<dono>@<id>/<repo>@<id>`); obtenha o seu com `gh api repos/<dono>/<repo>/actions/oidc/customization/sub --jq .sub_claim_prefix`. Em um fork, passe `-var prefixo_sub_oidc=<prefixo>` e `-var nome_bucket_state=<nome-unico>`, e ajuste o `bucket` em `bootstrap/backend.tf` e em `ambiente/backend.tf`.

## Passo a passo do bootstrap

Rode da sua máquina. Nada disso roda no CI.

1. Aplique com state local:

   ```
   cd infra/bootstrap
   terraform init
   terraform plan
   terraform apply
   ```

   Se a AWS ainda não liberou o nome do bucket, use `-var nome_bucket_state=15soat-tech-challenge-fiap-leonardo-sanches-tfstate` e ajuste o `bucket` em `backend.tf.apos-apply`.

2. Migre o state para o bucket criado:

   ```
   mv backend.tf.apos-apply backend.tf
   terraform init -migrate-state
   ```

   Confirme a migração e apague os `terraform.tfstate*` locais.

3. Cadastre as Variables do repositório no GitHub:

   ```
   gh variable set AWS_ROLE_ARN --body "$(terraform output -raw arn_role_ci)"
   gh variable set ARN_USUARIO_DESENVOLVEDOR --body "$(aws sts get-caller-identity --query Arn --output text)"
   ```

   `AWS_ROLE_ARN` é a role que o pipeline assume por OIDC. `ARN_USUARIO_DESENVOLVEDOR` é o usuário IAM que recebe acesso de administrador ao cluster (access entry).

4. Valide: `terraform plan` deve mostrar "No changes".

5. Opcional, para e-mail real em vez do Mailpit: cadastre os Secrets `SMTP_HOST`, `SMTP_PORTA`, `SMTP_USUARIO`, `SMTP_SENHA` e `SMTP_TLS` (`gh secret set`). Se `SMTP_HOST` estiver preenchido, o pipeline injeta essas opções na API; sem ele, a API usa o Mailpit do cluster.

### Trust da role do CI

A trust da role `oficina-ci` aceita apenas a branch `main` (`branches_permitidas` em `bootstrap/variaveis.tf`), e o `cd.yml` só dispara na `main`. A branch `integration/infra-fase-2`, usada para provar o pipeline antes do merge, foi retirada dos dois. **Depois do merge, rode `terraform apply` em `infra/bootstrap` para a AWS refletir a trust sem a branch de integração**; até isso ser feito, a role ainda aceita a branch antiga.

## Como o pipeline aplica tudo

`cd.yml` roda a cada push na `main`:

1. Build e testes com o gate de cobertura (Domain + Application >= 80%).
2. Login na AWS por OIDC (role `oficina-ci`).
3. Build e push das imagens `oficina-api` e `oficina-web` no ECR, com a tag do SHA do commit.
4. `terraform apply` da stack `ambiente`: rede, EKS, RDS, Traefik e segredos.
5. Geração dos arquivos `.env` do Kustomize a partir dos outputs do Terraform (endereço do load balancer, endpoint e credenciais do RDS, segredos gerados). Eles estão no `.gitignore`; só os `.env.example` são versionados.
6. Aplicação do Job de migration e da carga de demonstração, e espera pela conclusão.
7. Aplicação dos demais manifestos (`kubectl kustomize --load-restrictor LoadRestrictionsNone`) e espera dos rollouts. A flag é necessária porque o Kustomize lê o realm do Keycloak em `keycloak/oficina-realm.json`, fora do diretório dos manifestos.
8. Smoke test: `GET /saude` pelo DNS do load balancer, com retentativas até responder 200.

`ci.yml`, em PRs para a `main`, valida a infraestrutura sem tocar na AWS: `terraform fmt -check`, `terraform validate` das duas stacks, `kubectl kustomize` e `kubeconform`.

`cd.yml` e `derrubar-ambiente.yml` compartilham o grupo de `concurrency` `ambiente`, então nunca rodam juntos.

## Derrubar o ambiente

O workflow `derrubar-ambiente.yml` roda `terraform destroy` da stack `ambiente`. Dispara de duas formas:

- **Cron:** todo dia às 06:00 UTC (03:00 em Brasília). Se não houver nada de pé, o destroy é um no-op.
- **Manual:** `gh workflow run derrubar-ambiente.yml` ou pela aba Actions.

Como o Traefik e o load balancer estão no Terraform, o destroy remove o LB antes da VPC e não precisa de nenhum passo com `kubectl`. A stack `bootstrap` nunca é destruída pelo cron.

## Senhas e endereços

As senhas existem só no state. Para lê-las (na pasta `infra/ambiente`, com o ambiente de pé):

```
terraform init
terraform output -raw senha_rds
terraform output -raw senha_admin_keycloak
terraform output -raw senha_mailpit
terraform output -raw dns_load_balancer
```

Configurar o `kubectl`:

```
aws eks update-kubeconfig --name oficina --region us-east-1
kubectl get pods,hpa,svc -n oficina
```

Com `DNS` igual ao output `dns_load_balancer` (HTTP, sem domínio):

| Serviço | Endereço | Acesso |
|---|---|---|
| Interface web | `http://DNS/` | login pelo Keycloak (usuários do realm de demonstração) |
| API | `http://DNS/api` | JWT do Keycloak |
| Saúde | `http://DNS/saude` | anônimo |
| Swagger | `http://DNS/swagger` | login pelo Keycloak |
| Keycloak | `http://DNS/auth` | usuário `admin`, senha em `senha_admin_keycloak` |
| Mailpit | `http://DNS/mailpit` | basic auth: usuário `oficina`, senha em `senha_mailpit` |

## Teste de carga

O roteiro k6 está em [`carga/`](carga/README.md). O workflow `carga.yml` (`workflow_dispatch`) roda contra o load balancer do ambiente; localmente: `k6 run -e BASE_URL=http://DNS infra/carga/carga.js`. Acompanhe com `kubectl get hpa -n oficina -w`.

## Estimativa de custo

Cerca de **US$ 0,27 por hora** com o ambiente de pé:

| Item | Observação |
|---|---|
| Control plane do EKS | cobrado por hora de cluster |
| 2 nodes `c7i-flex.large` | desejado 2; o HPA pode levar a 3 |
| RDS `db.t4g.micro` | single-AZ, 20 GB |
| Load balancer | um só, do Traefik |
| IPs públicos | um por node |

Sem NAT Gateway, que seria o item mais caro. Deixar ligado um dia inteiro custa por volta de US$ 6,50; por isso o cron derruba tudo às 03:00 de Brasília. Os valores são estimativas; confira os preços vigentes da região `us-east-1`.

## Roteiro do vídeo

1. **Push na `main`:** mostrar o commit e abrir a aba Actions com o `cd.yml` rodando.
2. **Pipeline:** build e testes, push das imagens no ECR, `terraform apply`, Job de migration e smoke test. Cobre Docker e Terraform.
3. **Console da AWS:** mostrar o cluster EKS e o node group, a instância RDS (banco), os repositórios ECR, a VPC e o load balancer.
4. **`kubectl get`:** `kubectl get pods,svc,hpa,ingress -n oficina` (manifestos e HPA) e `kubectl get jobs -n oficina` (migration concluída).
5. **Uso do sistema:** entrar na interface web, autenticar no Keycloak, abrir uma OS, mostrar o Swagger e `GET /saude`.
6. **E-mail:** mudar o Status de uma OS e abrir `http://DNS/mailpit` para ver a notificação.
7. **Carga k6:** disparar o workflow `carga.yml` (ou `k6 run` local).
8. **HPA:** `kubectl get hpa -n oficina -w` mostrando a API escalando de 2 até 5 réplicas e voltando ao mínimo depois da carga.
9. **Derrubar o ambiente:** disparar `derrubar-ambiente.yml`, mostrar o destroy concluindo e o console da AWS sem os recursos do ambiente. Comentar o cron das 03:00.

Cobertura dos itens obrigatórios: Docker (imagens e ECR), manifestos (`infra/k8s`), HPA (passo 8), Terraform (passo 2 e stacks), banco (RDS e migration) e pipeline (passos 1 e 2).
