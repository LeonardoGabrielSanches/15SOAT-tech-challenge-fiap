# Oficina — Sistema Integrado de Atendimento e Execução de Serviços

MVP do back-end de uma oficina mecânica de médio porte, desenvolvido para o Tech Challenge da Fase 1 (SOAT — Software Architecture, FIAP).

## Objetivo

Substituir anotações manuais e planilhas por um sistema que conduz a Ordem de Serviço (OS) da recepção à entrega do veículo, com controle de estoque de peças e insumos, orçamento aprovado pelo cliente e acompanhamento do andamento por API.

## Descrição da solução

- O **Atendente** identifica o Cliente por CPF/CNPJ, cadastra o Veículo e abre a OS já com ao menos um Serviço.
- O **Mecânico** inicia o diagnóstico, ajusta Serviços e Peças/insumos (a inclusão dá baixa imediata no estoque) e conclui o diagnóstico.
- Ao concluir o diagnóstico, o sistema gera uma **Versão do orçamento** com preços congelados e a envia ao Cliente com um **Token de aprovação** de uso único. O envio é simulado: o e-mail, o Número da OS, o total e o token são registrados em log.
- O **Cliente** não tem conta: consulta a OS pelo Número da OS + CPF/CNPJ e aprova ou recusa o orçamento com o token.
- O **Mecânico** executa e finaliza; o **Atendente** registra a entrega.
- O **Gerente** mantém serviços, peças/insumos e estoque, acompanha o tempo médio de execução e pode executar qualquer ação administrativa.

### Status da OS

Recebida → Em diagnóstico → Aguardando aprovação → **Aprovada** → Em execução → Finalizada → Entregue, mais **Cancelada**.

| De | Ação | Para | Quem |
|---|---|---|---|
| — | Abrir OS | Recebida | Atendente |
| Recebida | Iniciar diagnóstico | Em diagnóstico | Mecânico |
| Em diagnóstico | Concluir diagnóstico | Aguardando aprovação | Mecânico |
| Aguardando aprovação | Aprovar orçamento | Aprovada | Cliente (token) |
| Aguardando aprovação | Recusar orçamento | Em diagnóstico | Cliente (token) |
| Aprovada | Iniciar execução | Em execução | Mecânico |
| Em execução | Finalizar execução | Finalizada | Mecânico |
| Finalizada | Registrar entrega | Entregue | Atendente |
| Recebida, Em diagnóstico ou Aguardando aprovação | Cancelar (motivo obrigatório) | Cancelada | Atendente |

**Por que Aprovada e Cancelada, além dos seis status do enunciado?** O enunciado lista seis status obrigatórios, mas não proíbe outros. *Aprovada* separa a decisão do Cliente do início real do trabalho: o Mecânico enxerga uma fila de OS aprovadas e escolhe a próxima, em vez de a aprovação já significar "em execução". *Cancelada* dá um fim explícito às OS abandonadas antes da execução; o cancelamento exige motivo, estorna ao estoque tudo o que foi baixado e invalida o token pendente. Os seis status obrigatórios continuam todos presentes, com os mesmos nomes.

### Fora do MVP

- Reparos adicionais durante a execução (não há transição de Em execução para Aguardando aprovação).
- Pagamento e qualquer condição financeira para a entrega.
- Envio real de notificações (e-mail/SMS/aplicativo) e notificação de OS Finalizada.
- Extrato de movimentações de estoque, alerta de estoque mínimo e ajuste de inventário.
- Descontos no orçamento.
- Login ou conta de Cliente.
- Cancelamento de OS Aprovada, Em execução ou posterior.
- Tempo de execução por Serviço individual (o tempo é da OS inteira; o filtro por Serviço seleciona as OS que o contêm).
- Gestão de usuários pela API (feita no Keycloak).

## Arquitetura

Monólito em camadas, com o domínio no centro. As dependências sempre apontam para o `Domain`.

| Camada | Projeto | Responsabilidade |
|---|---|---|
| Domain | `src/Oficina.Domain` | Entidades, objetos de valor, eventos de domínio e interfaces de repositório. Não referencia nenhum outro projeto nem pacote de infraestrutura. |
| Application | `src/Oficina.Application` | Casos de uso, que orquestram agregados e definem a transação (um único `SaveChanges` por caso de uso), e as interfaces de serviços técnicos (notificador, unidade de trabalho). |
| Infrastructure | `src/Oficina.Infrastructure` | EF Core/Npgsql, repositórios, migrations, gerador do Número da OS e notificador simulado. |
| Api | `src/Oficina.Api` | Endpoints REST (Minimal API), autenticação JWT via Keycloak, OpenAPI/Swagger e tratamento de erros (ProblemDetails). |

### Bounded contexts

| Contexto | Tipo | Conteúdo |
|---|---|---|
| Atendimento | Core | Cliente, Veículo, Ordem de Serviço (com orçamento, itens e Histórico de status) |
| Catálogo e Estoque | Supporting | Serviço, Peça/insumo (com saldo), Entrada de mercadoria, Baixa e Estorno |
| Identidade e Acesso | Generic | Keycloak (externo ao código): usuários e papéis |

A `OrdemDeServico` é o único agregado do fluxo e é gravada na mesma transação que o saldo da `PecaInsumo`. Baixa, estorno e Histórico de status são executados de forma síncrona pelo caso de uso; os eventos de domínio são despachados após o commit e hoje só `DiagnosticoConcluido` tem handler (envio do orçamento pelo notificador).

Erros de domínio retornam ProblemDetails: `404` para recurso inexistente ou consulta pública sem correspondência, `409` para conflito de estado ou concorrência, `422` para dados inválidos (CPF, placa, quantidade, saldo insuficiente).

## Tecnologias

- C# / .NET 10, ASP.NET Core Minimal API, OpenAPI nativo + Swagger UI
- Entity Framework Core + Npgsql, PostgreSQL 17
- Keycloak 26.2 (JWT, papéis `atendente`, `mecanico`, `gerente`)
- xUnit, Moq, Testcontainers (PostgreSQL), `WebApplicationFactory`
- coverlet + ReportGenerator (cobertura)
- Interface web: Next.js (App Router) + TypeScript, Tailwind CSS, shadcn/ui, Auth.js e openapi-fetch
- Docker / Docker Compose
- GitHub Actions (build, testes, cobertura, Trivy e `dotnet list package --vulnerable`)

## Banco de dados: por que PostgreSQL

O domínio é fortemente relacional (Cliente → Veículo → OS → itens → Serviço e Peça/insumo) e as regras de consistência dependem de transações ACID que envolvem mais de um agregado: incluir uma peça numa OS altera a OS e o saldo da peça na mesma transação. Por isso descartamos bancos não relacionais. O PostgreSQL é open source, sem custo, oferece transações ACID, o tipo `numeric` para dinheiro e quantidades fracionárias, sequências (usadas no Número da OS, `OS-AAAA-NNNNN`, reiniciado a cada ano), a coluna de sistema `xmin` (concorrência otimista no saldo) e um provedor maduro para .NET (Npgsql/EF Core). Tem imagem oficial para o `docker-compose.yml`. As regras de negócio vivem no domínio; restrições do banco (placa única, documento único, índice único parcial de uma OS em andamento por veículo) são apenas uma segunda barreira contra concorrência.

## Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) com Docker Compose v2 (suficiente para subir todo o ambiente).
- Para rodar fora do Docker ou executar os testes: [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0). Os testes de integração usam Testcontainers e precisam de um Docker em execução.

## Como executar

```bash
docker compose up --build
```

Sobe PostgreSQL (porta 5432), Keycloak (porta 8080, com o realm `oficina` importado automaticamente), a API (porta **8081**) e a interface web (porta **3000**). A API só inicia depois que os dois ficam saudáveis (o Keycloak leva cerca de um minuto na primeira vez). Ao iniciar, a API aplica as migrations e carrega dados de demonstração de forma idempotente.

Verificação rápida:

```bash
curl -i http://localhost:8081/saude
```

Para derrubar o ambiente (e apagar o volume do banco): `docker compose down -v`.

### Executando a API fora do Docker

Suba só as dependências e rode a API (porta 5233, com a carga de demonstração ligada pelo `appsettings.Development.json`):

```bash
docker compose up -d postgres keycloak
dotnet run --project src/Oficina.Api
```

Neste modo o botão Authorize do Swagger não funciona (o redirecionamento OAuth do realm só está cadastrado para as portas 8081 e 5000); obtenha o token pela linha de comando, como descrito abaixo.

## Interface web

A interface web (`web/`, Next.js) atende os funcionários e é a forma recomendada de usar a oficina no dia a dia. Com o ambiente de pé, acesse <http://localhost:3000>: o navegador é levado ao login do Keycloak e volta para a área administrativa com o nome e o papel do funcionário no topo, um menu montado para o papel e a tela inicial do papel (Balcão para o Atendente, Fila de execução para o Mecânico, Serviços para o Gerente). Use os usuários de demonstração da seção abaixo. O botão **Sair** encerra a sessão da interface e a do Keycloak.

A interface funciona como BFF ([ADR 0007](docs/adr/0007-interface-web-como-bff.md)): o login usa authorization code + PKCE no client confidencial `oficina-web`, o JWT fica num cookie criptografado da sessão do servidor e toda chamada à API parte do servidor Next.js. O navegador nunca vê o token, e a API continua sem CORS. O secret do client `oficina-web` (`oficina-web-secret-somente-dev`) e o `AUTH_SECRET` versionados no realm, no `docker-compose.yml` e em `web/.env.development` são **exclusivos do ambiente de demonstração local**.

### Executando a interface fora do Docker

Requer Node.js 22 ou superior. Suba as dependências e rode a interface em modo de desenvolvimento (porta 3000, configurada por `web/.env.development`):

```bash
docker compose up -d postgres keycloak api
cd web
npm install
npm run dev
```

Outros comandos em `web/`:

| Comando | O que faz |
|---|---|
| `npm run typecheck` | gera os tipos das rotas e roda `tsc --noEmit` |
| `npm run lint` | ESLint |
| `npm run build` | build de produção (standalone) |
| `npm run api:tipos` | regenera `lib/api/openapi.ts` a partir de <http://localhost:8081/openapi/v1.json> (API no ar) |

Variáveis de ambiente da interface (definidas no `docker-compose.yml` e em `web/.env.development`): `API_URL`, `KEYCLOAK_URL_PUBLICA` (usada pelo navegador e como issuer), `KEYCLOAK_URL_INTERNA` (usada pelo servidor Next.js para trocar código e refresh token), `KEYCLOAK_CLIENT_ID`, `KEYCLOAK_CLIENT_SECRET`, `AUTH_SECRET` e `AUTH_URL`.

## Swagger

Com o ambiente de pé: <http://localhost:8081/swagger> (a raiz `/` redireciona para ele). O documento OpenAPI está em <http://localhost:8081/openapi/v1.json>.

Para autenticar pela interface, clique em **Authorize**, use o client `oficina-swagger` (público, com PKCE) e faça login no Keycloak com um dos usuários abaixo.

## Autenticação e usuários de demonstração

As APIs administrativas (`/api/administrativo/**` e `/api/pecas-insumos/**`) exigem JWT emitido pelo Keycloak. Os endpoints do Cliente (`/api/publico/**`) são públicos.

| Usuário | Senha | Papel | O que pode |
|---|---|---|---|
| `atendente` | `atendente123` | Atendente | Clientes, veículos, abrir/listar OS, cancelar, registrar entrega |
| `mecanico` | `mecanico123` | Mecânico | Diagnóstico, peças/insumos da OS, execução, fila de OS aprovadas |
| `gerente` | `gerente123` | Gerente | Catálogo, estoque, tempo médio; passa em todas as políticas |

Obtenção do token (fluxo *password* do client `oficina-swagger`, apenas para demonstração):

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/realms/oficina/protocol/openid-connect/token \
  -d grant_type=password -d client_id=oficina-swagger \
  -d username=atendente -d password=atendente123 | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])")

curl -H "Authorization: Bearer $TOKEN" http://localhost:8081/api/administrativo/clientes
```

Credenciais do console do Keycloak (<http://localhost:8080>): `admin` / `admin`. Todas as credenciais acima são exclusivas do ambiente de demonstração local.

### Dados de demonstração

Carregados na inicialização (sem duplicar em reinícios):

- Serviços: Troca de óleo (R$ 120,00), Alinhamento e balanceamento (R$ 150,00), Revisão de freios (R$ 200,00).
- Peças/insumos: Filtro de óleo (peça, R$ 35,00, saldo 20 un) e Óleo 5W30 (insumo, R$ 45,00, saldo 50,5 L).
- Cliente Maria Silva, CPF `529.982.247-25`, com o Veículo Fiat Argo 2021, placa `BRA2E19`.

Com isso é possível abrir uma OS imediatamente.

### Principais endpoints

| Endpoint | Papel |
|---|---|
| `POST/GET/PUT/DELETE /api/administrativo/clientes` | Atendente |
| `POST/GET/PUT/DELETE /api/administrativo/veiculos`, `POST .../veiculos/{id}/transferencia`, `GET .../clientes/{id}/veiculos` | Atendente |
| `GET/POST/PUT/DELETE /api/administrativo/servicos` | leitura: qualquer papel; escrita: Gerente |
| `POST/GET/PUT/DELETE /api/pecas-insumos`, `POST .../{id}/entradas`, `GET .../{id}/saldo` | escrita: Gerente; leitura: Mecânico |
| `POST/GET /api/administrativo/ordens-de-servico` (abrir, listar com filtro `status`) | Atendente |
| `GET /api/administrativo/ordens-de-servico/{id}` (detalhe) | Atendente ou Mecânico |
| `POST/DELETE .../ordens-de-servico/{id}/servicos` | Atendente ou Mecânico |
| `POST .../{id}/diagnostico/iniciar`, `.../diagnostico/concluir` | Mecânico |
| `POST/DELETE .../{id}/pecas-insumos` | Mecânico |
| `GET .../ordens-de-servico/fila-de-execucao`, `POST .../{id}/execucao/iniciar`, `.../execucao/finalizar` | Mecânico |
| `POST .../{id}/entrega`, `POST .../{id}/cancelar` | Atendente |
| `GET .../ordens-de-servico/tempo-medio-de-execucao?servicoId=` | Gerente |
| `POST /api/publico/ordens-de-servico/consulta` (`numero` + `documento`) | público |
| `POST /api/publico/orcamentos/{token}/aprovar`, `.../recusar` | público (token) |

O Gerente é aceito em todos os endpoints administrativos. A listagem de OS oculta Entregue e Cancelada por padrão e ordena por prioridade (Em execução, Aprovada, Aguardando aprovação, Em diagnóstico, Recebida, Finalizada), da mais antiga para a mais nova dentro do status.

## Testes e cobertura

```bash
dotnet test Oficina.sln
```

Os testes de integração sobem um PostgreSQL real via Testcontainers (Docker em execução) e usam um handler de autenticação de teste: o Keycloak não é necessário.

- **Unitários** (`tests/Oficina.UnitTests`): casos de uso com repositórios e notificador como mocks Moq, objetos de valor e regras do domínio.
- **Integração** (`tests/Oficina.IntegrationTests`): API HTTP com PostgreSQL real e migrations aplicadas, cobrindo o ciclo completo da OS, recusa, cancelamento com estorno, falta de saldo, OS em andamento duplicada, token reutilizado, consulta pública, 401/403 e inativação.

Cobertura (coverlet + ReportGenerator) com gate de 80% em Domain + Application:

```bash
bash scripts/cobertura.sh
```

O script roda a suíte, gera o relatório em `coverage/` (abra `coverage/index.html`) e falha se a cobertura de linhas ficar abaixo de 80% (ajustável por `LIMITE_COBERTURA`). Na última execução (commit `648183e`, 272 testes unitários e 93 de integração, todos aprovados): **98,6% de linhas em Domain + Application** (Domain 97,5%, Application 99,6%). Infrastructure e Api são cobertas pelos testes de integração, sem meta percentual.

## Integração contínua e segurança

Em todo PR para a `main`:

- `.github/workflows/ci.yml`: build em Release, testes e gate de cobertura; publica o relatório como artefato.
- `.github/workflows/vulnerabilidades.yml`: Trivy na imagem da API e nas dependências do repositório, e `dotnet list package --vulnerable --include-transitive`. O fluxo falha para vulnerabilidade CRITICAL com correção disponível ou pacote vulnerável; os relatórios são sempre publicados.

O relatório final consolidado está em [`docs/seguranca/relatorio-vulnerabilidades.md`](docs/seguranca/relatorio-vulnerabilidades.md), com os relatórios brutos na mesma pasta.

## Estrutura de diretórios

```
.
├── src/
│   ├── Oficina.Domain/          entidades, objetos de valor, eventos e interfaces (por contexto)
│   ├── Oficina.Application/     casos de uso, notificador, unidade de trabalho, carga de demonstração
│   ├── Oficina.Infrastructure/  EF Core, repositórios, migrations, notificador simulado
│   └── Oficina.Api/             endpoints, autenticação Keycloak, OpenAPI, tratamento de erros
├── tests/
│   ├── Oficina.UnitTests/       testes unitários (Moq)
│   └── Oficina.IntegrationTests/ testes de integração (Testcontainers)
├── docs/
│   ├── ddd/                     Event Storming, diagramas e linguagem ubíqua
│   └── seguranca/               relatório de vulnerabilidades e relatórios brutos
├── web/                         interface web (Next.js, BFF)
├── keycloak/oficina-realm.json  realm importado (papéis, usuários de demonstração, clients do Swagger e da interface web)
├── scripts/cobertura.sh         testes + gate de cobertura
├── .github/workflows/           ci.yml e vulnerabilidades.yml
├── Dockerfile                   build multi-stage da API
├── docker-compose.yml           PostgreSQL + Keycloak + API + interface web
├── GLOSSARY.md                  glossário do domínio
└── Oficina.sln
```

## Documentação DDD

- [Linguagem ubíqua e relação termo → código](docs/ddd/linguagem-ubiqua.md)
- [Event Storming](docs/ddd/event-storming.md) (quadro em [PDF](docs/ddd/event-storming.pdf) e [imagem](docs/ddd/event-storming.png)) e [alterações aplicadas ao quadro](docs/ddd/alteracoes-event-storming.md)
- [Diagramas](docs/ddd/diagramas.pdf): agregados, bounded contexts e máquina de status da OS
- [Glossário](GLOSSARY.md)
- Quadro online (Miro ou equivalente): **PENDENTE — link público a ser inserido pelo grupo**

## Vídeo da entrega

**PENDENTE — link do vídeo público no YouTube (máximo de 15 minutos) a ser inserido pelo grupo:** `<LINK_DO_VIDEO>`
