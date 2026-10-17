# Relatório final de análise de vulnerabilidades

## Identificação

| Item | Valor |
|---|---|
| Commit analisado | `648183e25413dda010c2abad4519f2f4e4244b01` (`integration/oficina-mvp`) |
| Data da análise | 17/10/2026 |
| Escopo | Imagem Docker da API (`Dockerfile`), dependências NuGet de todos os projetos da solução e arquivos do repositório |
| Execução | Local, com os mesmos parâmetros do workflow `.github/workflows/vulnerabilidades.yml` |

O workflow executa a mesma análise em todo PR para a `main` e publica os relatórios como artefato `relatorios-vulnerabilidades`. Este relatório consolida a execução feita no commit acima; a execução definitiva sobre o commit de entrega fica a cargo do CI (ver "Pendências").

## Ferramentas

| Ferramenta | Versão | Comando |
|---|---|---|
| Trivy (imagem) | 0.74.0 | `trivy image --severity CRITICAL,HIGH --ignore-unfixed oficina-api:scan` |
| Trivy (sistema de arquivos e dependências) | 0.74.0 | `trivy fs --severity CRITICAL,HIGH --ignore-unfixed .` |
| .NET SDK | 10.0.101 | `dotnet list Oficina.sln package --vulnerable --include-transitive` |

A imagem foi construída com `docker build --pull --no-cache -t oficina-api:scan .`.

## Resumo por severidade

Resultado final, no commit analisado:

| Análise | CRITICAL | HIGH | Relatório bruto |
|---|---|---|---|
| Trivy, imagem da API (SO e runtime .NET) | 0 | 0 | [`trivy-imagem.txt`](trivy-imagem.txt), [`trivy-imagem.sarif`](trivy-imagem.sarif) |
| Trivy, dependências e arquivos do repositório | 0 | 0 | [`trivy-fs.txt`](trivy-fs.txt), [`trivy-fs.json`](trivy-fs.json) |
| `dotnet list package --vulnerable` (incluindo transitivos), 6 projetos | 0 pacotes vulneráveis | | [`dotnet-vulnerable.txt`](dotnet-vulnerable.txt) |

Os gates do CI foram reproduzidos localmente: o Trivy com `--severity CRITICAL --ignore-unfixed --exit-code 1` na imagem terminou com código 0.

## Itens corrigidos

Uma primeira análise, feita com uma imagem base `mcr.microsoft.com/dotnet/aspnet:10.0` em cache desatualizada na máquina, encontrou 14 vulnerabilidades com correção disponível, todas na imagem base e nenhuma no código ou nos pacotes NuGet da aplicação ([`trivy-imagem-base-desatualizada.txt`](trivy-imagem-base-desatualizada.txt)):

| Componente | Versão instalada | Severidade | Quantidade | Correção |
|---|---|---|---|---|
| ASP.NET Core Runtime (`Microsoft.AspNetCore.App`) | 10.0.12 | 2 CRITICAL, 5 HIGH | 7 | 10.0.13 |
| .NET Runtime (`Microsoft.NETCore.App`) | 10.0.12 | 5 HIGH | 5 | 10.0.13 |
| `openssl` e `libssl3t64` (Ubuntu 24.04) | 3.0.13-0ubuntu3.15 | 2 HIGH (CVE-2026-84782) | 2 | 3.0.13-0ubuntu3.16 |

Correção: reconstruir a imagem com `docker build --pull`, que traz a base atualizada (runtime 10.0.13 e pacotes do Ubuntu corrigidos). Nenhuma alteração de código foi necessária. Após o rebuild, o Trivy reportou 0 vulnerabilidades CRITICAL/HIGH com correção disponível, incluindo a lista de dependências da própria API (`Oficina.Api.deps.json`, 0 achados).

## Itens aceitos com justificativa

| Item | Justificativa |
|---|---|
| Vulnerabilidades sem correção disponível (`--ignore-unfixed`) | Não são reportadas pelo gate, pois não há ação possível além de aguardar o fornecedor. Em uma verificação complementar com `--severity CRITICAL,HIGH,MEDIUM,LOW --ignore-unfixed` na imagem corrigida, o Trivy não listou nenhum achado fixável. Vulnerabilidades sem correção, se existirem, não foram contabilizadas. |
| Severidades MEDIUM e LOW | Fora do escopo do relatório (a análise filtra CRITICAL e HIGH, como o workflow). Nenhum achado MEDIUM ou LOW com correção disponível foi listado na verificação complementar acima. |
| Credenciais de demonstração versionadas (`keycloak/oficina-realm.json`, `docker-compose.yml`, `appsettings.json`) | Senhas dos usuários de demonstração, `admin/admin` do Keycloak e `oficina/oficina` do PostgreSQL existem apenas para o ambiente local de avaliação, conforme o README. O Trivy não detectou segredos além destes (scanner de segredos sem achados no `trivy fs`). Em produção, devem ser substituídas por segredos do ambiente. |
| Fluxo *password* (`directAccessGrantsEnabled`) no client público `oficina-swagger` | Mantido para que o avaliador obtenha o token por linha de comando. É uma conveniência de demonstração e deve ser desligado fora dela. |
| Keycloak em `start-dev` e HTTP sem TLS (`ExigirHttps: false`) | Perfil de desenvolvimento do ambiente local do `docker-compose.yml`. Um ambiente real exige `start` com TLS. |

## Observações sobre o processo

- Os resultados dependem da base de vulnerabilidades do momento da execução; o CI deve ser a fonte da análise sobre o commit de entrega.
- Em máquinas com locale diferente do inglês, a saída de `dotnet list package` é traduzida; o passo equivalente do workflow, executado em `ubuntu-latest` em inglês, procura o texto `has the following vulnerable packages`. Nesta execução local (locale pt-BR) a ausência de pacotes vulneráveis foi conferida lendo a saída.
- O workflow fixa a imagem base pela tag flutuante `10.0`; por isso um build sem `--pull` em máquina com cache antigo pode reproduzir os achados da tabela de itens corrigidos.

## Pendências

- Reexecutar o workflow `Vulnerabilidades` no PR para a `main` sobre o commit final da entrega e anexar/atualizar o resultado, caso o hash final difira de `648183e`.
