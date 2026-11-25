# Infraestrutura na AWS com EKS sob demanda e destroy automático

A Oficina roda na AWS num cluster EKS criado por Terraform a cada push na `main` e destruído toda madrugada pelo pipeline. Duas stacks separam o que é permanente (bucket do state, OIDC do GitHub, role do CI e ECR) do que é descartável (rede, EKS, RDS e Traefik). O pipeline assume a role do CI por OIDC, sem chaves de acesso guardadas no GitHub.

As escolhas que sustentam esse desenho:

- **Nuvem em vez de cluster local.** O enunciado permite os dois. A nuvem mostra o pipeline de ponta a ponta (imagem no registro, `terraform apply`, rollout e smoke test por um endereço público) e deixa o ambiente reproduzível por qualquer avaliador com uma conta AWS.
- **Sem NAT Gateway.** Os nodes ficam em subnets públicas e só o RDS fica em subnets privadas. O NAT seria o maior custo fixo do ambiente e nada no cluster precisa de saída privada.
- **RDS em vez de StatefulSet.** O banco gerenciado dispensa operar volumes e backups dentro do cluster, e o mesmo RDS serve à API e ao Keycloak (bancos `oficina` e `keycloak`).
- **Um único load balancer, com Traefik.** O Traefik é instalado pelo Terraform com Service `LoadBalancer`; o Ingress roteia web, API, Swagger, Keycloak e Mailpit por caminho. Por estar no Terraform, o destroy remove o LB antes da VPC, sem passos de `kubectl`.
- **HTTP sem domínio.** O endereço público é o DNS do load balancer. Domínio e certificado custariam tempo e dinheiro sem ganho para a avaliação.
- **Nodes x86 (`c7i-flex.large`).** O Free plan só oferece instâncias Graviton de até 2 GB, pequenas demais para API, web, Keycloak e Mailpit juntos.
- **Segredos gerados pelo Terraform.** Senha do RDS, admin do Keycloak, client secret da web, `AUTH_SECRET` e senha do Mailpit saem de `random_password` e vivem só no state, que é cifrado e privado. O pipeline os entrega ao cluster por Secrets do Kubernetes gerados pelo Kustomize.

## Considered Options

- **Cluster local (kind, minikube ou k3d)**: custo zero, mas não exercita provedor de nuvem, OIDC nem registro de imagens.
- **Ambiente permanente**: simples, mas os créditos do Free plan não cobrem um cluster ligado o mês inteiro.
- **NAT Gateway para nodes privados**: mais isolamento, a um custo fixo desproporcional.
- **PostgreSQL em StatefulSet**: evita o RDS, mas leva a operação de armazenamento e backup para dentro do cluster.
- **Secrets Manager com External Secrets Operator**: é a evolução natural para os segredos, mas acrescenta um operador e permissões IAM que este escopo não precisa.

## Consequences

- O ambiente custa cerca de US$ 0,27 por hora de pé; o cron de destroy limita o gasto.
- Os dados do RDS não sobrevivem ao destroy: o Job de migration recria o esquema e a carga de demonstração a cada implantação (ADR 0012).
- Sem HTTPS, o tráfego e os cookies de sessão circulam em texto claro; é aceitável para uma demonstração e inaceitável para produção.
- Os segredos aparecem no state e, descriptografados, no cluster; a migração para Secrets Manager com External Secrets Operator fica como evolução.
- O `terraform apply` do bootstrap é manual, uma vez, pelo desenvolvedor; depois dele, só a `main` assume a role do CI.
