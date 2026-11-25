# Aplicar o bootstrap

Rode da sua maquina, com o usuario IAM ja logado (`aws sts get-caller-identity`) e Terraform >= 1.10. Nada aqui roda no CI.

1. Aplique com state local:

   ```
   cd infra/bootstrap
   terraform init
   terraform plan
   terraform apply
   ```

   Se a AWS ainda nao liberou o nome do bucket, use `-var nome_bucket_state=15soat-tech-challenge-fiap-leonardo-sanches-tfstate` e ajuste o `bucket` em `backend.tf.apos-apply`.

2. Migre o state para o bucket criado:

   ```
   mv backend.tf.apos-apply backend.tf
   terraform init -migrate-state
   ```

   Confirme a migracao e apague os `terraform.tfstate*` locais.

3. Cadastre o ARN da role no GitHub:

   ```
   gh variable set AWS_ROLE_ARN --body "$(terraform output -raw arn_role_ci)"
   ```

4. Valide:
   - `terraform plan` mostra "No changes".
   - Dispare o workflow "Teste OIDC" na branch `integration/infra-fase-2`: o `aws sts get-caller-identity` deve mostrar a role `oficina-ci`.
   - O mesmo workflow em outra branch deve falhar ao assumir a role.
   - Depois do teste, remova `.github/workflows/oidc-teste.yml` (ou substitua pelo workflow do ticket 04).
