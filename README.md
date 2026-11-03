# Oficina — Sistema Integrado de Atendimento e Execução de Serviços

MVP do back-end de uma oficina mecânica de médio porte, desenvolvido para o Tech Challenge da Fase 1 (SOAT — Software Architecture, FIAP) e refatorado na Fase 2. A seção [Fase 2](#fase-2) liga cada requisito da refatoração ao endpoint e ao caso de uso que o atendem.

## Objetivo

Substituir anotações manuais e planilhas por um sistema que conduz a Ordem de Serviço (OS) da recepção à entrega do veículo, com controle de estoque de peças e insumos, orçamento aprovado pelo cliente e acompanhamento do andamento por API.

## Descrição da solução

- O **Atendente** identifica o Cliente por CPF/CNPJ, cadastra o Veículo e abre a OS já com ao menos um Serviço.
- O **Mecânico** inicia o diagnóstico, ajusta Serviços e Peças/insumos (a inclusão dá baixa imediata no estoque) e conclui o diagnóstico.
- Ao concluir o diagnóstico, o sistema gera uma **Versão do orçamento** com preços congelados e a envia ao Cliente com um **Token de aprovação** de uso único.
- A cada mudança de Status da OS, inclusive a abertura, o Cliente recebe uma **Notificação de status** por e-mail (SMTP via MailKit, de melhor esforço). O e-mail de Aguardando aprovação traz os itens, o total e o link para a consulta pública.
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

### Tempo médio de execução

O tempo de execução de uma OS vai da entrada em Em execução até Finalizada, lido no Histórico de status, e entram no cálculo as OS Finalizadas ou Entregues. O tempo é **corrido** e medido para a **OS inteira**: noites, fins de semana, esperas e OS executadas em paralelo pelo mesmo Mecânico entram no intervalo, porque o Mecânico não registra início e fim de cada serviço.

O Gerente vê a média geral e a média de cada **Combinação de serviços**, que é o conjunto exato de Serviços da OS. A ordem dos Serviços não importa e as peças/insumos não entram. Assim, uma OS com troca de óleo e troca de pneu (55 min) não infla a média de uma OS só com troca de óleo (15 min): aparecem "Troca de óleo" com 15 min e "Troca de óleo + Troca de pneu" com 55 min. A lista vem da combinação mais frequente para a menos frequente (no empate, a de menor tempo médio), traz a quantidade de OS ao lado de cada média, não tem amostra mínima nem paginação, e mantém os Serviços inativos identificados como tais.

O requisito "tempo médio de execução dos serviços" é lido como o tempo dos serviços da oficina, medido por OS. A alternativa de medir cada serviço exigiria apontamento pelo Mecânico, e ratear o tempo da OS entre os serviços inventaria números ([ADR 0008](docs/adr/0008-tempo-de-execucao-por-os-recortado-por-combinacao.md)).

### Fora do MVP

- Reparos adicionais durante a execução (não há transição de Em execução para Aguardando aprovação).
- Pagamento e qualquer condição financeira para a entrega.
- Notificações por SMS ou aplicativo, e reenvio de e-mails que falharam (o envio é de melhor esforço, ADR 0010).
- Extrato de movimentações de estoque, alerta de estoque mínimo e ajuste de inventário.
- Descontos no orçamento.
- Login ou conta de Cliente.
- Cancelamento de OS Aprovada, Em execução ou posterior.
- Tempo de execução por Serviço individual (o tempo é da OS inteira e é comparado por Combinação de serviços).
- Gestão de usuários pela API (feita no Keycloak).

## Arquitetura

Monólito em camadas, com o domínio no centro. As dependências sempre apontam para o `Domain`.

| Camada | Projeto | Responsabilidade |
|---|---|---|
| Domain | `src/Oficina.Domain` | Entidades, objetos de valor, eventos de domínio e interfaces de repositório. Não referencia nenhum outro projeto nem pacote de infraestrutura. |
| Application | `src/Oficina.Application` | Casos de uso, que orquestram agregados e definem a transação (um único `SaveChanges` por caso de uso), e as interfaces de serviços técnicos (notificador, unidade de trabalho). |
| Infrastructure | `src/Oficina.Infrastructure` | EF Core/Npgsql, repositórios, migrations, gerador do Número da OS e notificador por e-mail (MailKit). |
| Api | `src/Oficina.Api` | Endpoints REST (Minimal API), autenticação JWT via Keycloak, OpenAPI/Swagger e tratamento de erros (ProblemDetails). |

### Bounded contexts

| Contexto | Tipo | Conteúdo |
|---|---|---|
| Atendimento | Core | Cliente, Veículo, Ordem de Serviço (com orçamento, itens e Histórico de status) |
| Catálogo e Estoque | Supporting | Serviço, Peça/insumo (com saldo), Entrada de mercadoria, Baixa e Estorno |
| Identidade e Acesso | Generic | Keycloak (externo ao código): usuários e papéis |

A `OrdemDeServico` é o único agregado do fluxo e é gravada na mesma transação que o saldo da `PecaInsumo`. Baixa, estorno e Histórico de status são executados de forma síncrona pelo caso de uso; os eventos de domínio são despachados após o commit e `StatusDaOsAlterado` tem handler (Notificação de status por e-mail).

Erros de domínio retornam ProblemDetails: `404` para recurso inexistente ou consulta pública sem correspondência, `409` para conflito de estado ou concorrência, `422` para dados inválidos (CPF, placa, quantidade, saldo insuficiente).

## Fase 2

O enunciado da Fase 2 pede duas coisas: refatorar o código da Fase 1 com Clean Code, Clean Architecture, SOLID, KISS, DRY e testes automatizados, e criar ou alterar cinco APIs da OS. Esta seção liga cada requisito ao endpoint e ao caso de uso que o atendem e registra as interpretações escolhidas. Os termos seguem o [glossário](GLOSSARY.md): a "situação" do requisito é o Status da OS, e "reprovar" é Recusar.

### Requisitos, endpoints e casos de uso

Os casos de uso ficam em `src/Oficina.Application`, um por arquivo, com o nome da classe. Os testes citados ficam em `tests/Oficina.IntegrationTests/Api/Atendimento`.

| Requisito | Endpoint | Caso de uso | Como verificar |
|---|---|---|---|
| Abertura de OS com cliente, veículo, serviços e peças | `POST /api/administrativo/ordens-de-servico` com `clienteId`, `veiculoId`, `servicoIds` e, opcionalmente, `pecasInsumos` (`[{ "pecaInsumoId", "quantidade" }]`) | `AbrirOs` | Responde `201` com o `id` e o `numero` (Número da OS). As peças/insumos recebem Baixa na mesma transação da OS, e o saldo cai em `GET /api/pecas-insumos/{id}/saldo`. Peça/insumo repetida, Inativa, sem saldo ou com fração em Peça é recusada sem criar OS nem baixar saldo. Testes: `AberturaComPecasInsumosTests`. |
| Consulta de status | `POST /api/publico/ordens-de-servico/consulta` (`numero` + `documento`) | `ConsultarOsPublica` | Devolve o Status da OS, o Histórico de status e o orçamento vigente. Os funcionários usam `GET /api/administrativo/ordens-de-servico/{id}` (`DetalharOs`). Testes: `ConsultaPublicaDaOsTests`. |
| Aprovação de orçamento por notificação externa | `POST /api/publico/orcamentos/{token}/decisao` com `{ "decisao": "Aprovar", "motivo": null }` ou `{ "decisao": "Recusar", "motivo": "..." }` | `AprovarOrcamento` ou `RecusarOrcamento`, conforme a `decisao` | `204` em caso de sucesso, `400` para `decisao` ausente ou inválida, `404` para token desconhecido e `409` para token usado, versão superada ou OS fora de Aguardando aprovação. O `motivo` é ignorado em Aprovar. Testes: `DecisaoDoOrcamentoTests`. |
| Listagem ordenada por status, das mais antigas para as mais novas, sem Finalizadas e Entregues | `GET /api/administrativo/ordens-de-servico?status=&pagina=&tamanho=` | `ListarOs`, sobre a consulta `IConsultaDeOrdensDeServico` | Responde o envelope `{ itens, pagina, tamanho, total }`. Sem `status`, traz só Em execução, Aprovada, Aguardando aprovação, Em diagnóstico e Recebida, nessa ordem, e as mais antigas primeiro dentro de cada status. Com `status`, traz exatamente aquele status, inclusive Finalizada, Entregue e Cancelada. `pagina` tem padrão 1 e mínimo 1; `tamanho` tem padrão 20 e vai de 1 a 100. Um número fora desses limites é dado inválido e recebe `422`; um valor que não é número inteiro (ex.: `pagina=abc`) é recusado antes, pelo binding da rota, com `400`. Testes: `ListagemDeOsTests`. |
| Atualização de status da OS por e-mail | toda mudança de Status da OS, inclusive a abertura | `NotificarClienteDaMudancaDeStatus`, manipulador do evento `StatusDaOsAlterado` | Com o docker-compose de pé, mude o status de uma OS e abra o Mailpit em <http://localhost:8025>. O passo a passo e a configuração de um SMTP real estão em [E-mails de Notificação de status](#e-mails-de-notificação-de-status). Testes: `NotificacaoDeStatusTests`. |

### O que mudou na refatoração

- **Camadas.** Continuam os quatro projetos da [ADR 0005](docs/adr/0005-camadas-com-dominio-independente.md), sem projetos novos e sem renomear para ports/adapters. Em termos de Clean Architecture, o `Domain` é o círculo das entidades, a `Application` é o dos casos de uso, e a `Infrastructure` e a `Api` são os adaptadores e frameworks. As dependências apontam para o `Domain`.
- **Um caso de uso por arquivo.** Os arquivos que reuniam vários casos de uso (`CasosDeUsoDeOrdemDeServico.cs`, `CasosDeUsoDeAprovacao.cs` e outros) viraram uma classe por arquivo, com o comando de entrada no mesmo arquivo. Os DTOs compartilhados ganharam arquivos próprios. As pastas agrupam por capacidade: `Atendimento/OrdensDeServico`, `Diagnostico`, `Orcamento`, `Execucao`, `Notificacao`, `Clientes`, `Veiculos`, além de `Catalogo` e `Estoque`.
- **Regras de status no domínio.** `RegrasDoStatusOs` responde, num único lugar, se um status é OS em andamento (`EstaEmAndamento`), se aparece na listagem padrão (`ApareceNaListagemPadrao`) e qual a prioridade na listagem (`PrioridadeNaListagem`). O repositório, a consulta e os casos de uso usam essas respostas em vez de repetir listas de status.
- **Repositório dividido entre escrita e consultas.** `IOrdemDeServicoRepository` (Domain) tem só o que os casos de uso de escrita usam: obter a OS por id, por Número da OS e por Token de aprovação, saber se o Veículo tem OS em andamento e adicionar. `IConsultaDeOrdensDeServico` (Application) faz as leituras: a listagem, projetada direto para o resumo, com filtro, ordenação e paginação no banco, sem carregar o agregado; a Fila de execução; e os tempos de execução do tempo médio. As duas são implementadas com EF Core (`OrdemDeServicoRepository` e `ConsultaDeOrdensDeServico`).
- **Duplicações removidas.**
  - As validações da abertura (ao menos um Serviço, Serviço repetido, Cliente e Veículo ativos, Cliente como Proprietário atual) existem só no agregado e rodam dentro de `OrdemDeServico.Abrir`. O caso de uso `AbrirOs` não as repete: o Número da OS é reservado na mesma transação da abertura, e uma abertura recusada desfaz a transação (`IUnidadeDeTrabalho.IniciarTransacao`), sem consumir o número.
  - A inclusão de peças/insumos na abertura usa a mesma regra da inclusão no diagnóstico (Baixa e soma de itens).
  - A carga das peças/insumos de uma OS, usada por `ConcluirDiagnostico`, `CancelarOs` e `DetalharOs`, ficou num único trecho (`PecasInsumosDaOs`).
  - O autor "Cliente" do Histórico de status e da consulta pública virou uma constante (`AutorDoHistorico.Cliente`).
- **Mantido por KISS.** O despachante de eventos baseado em reflection continua como está, os casos de uso não ganharam interfaces e o e-mail não tem tabela de saída (outbox).

### Análise SOLID

- **S (responsabilidade única).** O antigo "caso de uso de aprovação" fazia duas operações, Aprovar e Recusar, e por isso tinha dois motivos para mudar: a aprovação leva a OS para a Fila de execução, e a recusa registra um motivo e devolve a OS ao diagnóstico. Não estava de acordo com o S. Hoje são duas classes, `AprovarOrcamento` e `RecusarOrcamento`, cada uma no seu arquivo. O endpoint único de decisão só escolhe qual delas chamar. O mesmo vale para os demais casos de uso. No e-mail, `MensagemDeMudancaDeStatus` monta o texto e `NotificadorPorEmail` só envia.
- **O (aberto/fechado).** Um status novo muda o enum e `RegrasDoStatusOs`, sem caçar listas no repositório, na consulta e nos casos de uso. Um novo consumidor de evento entra como mais um `IManipuladorDeEvento<T>` registrado, sem mexer no agregado nem no despachante. Outro canal de notificação é outra implementação de `INotificador`.
- **L (substituição de Liskov).** Não há hierarquias com comportamento sobrescrito: `RaizDeAgregado` só guarda os eventos de domínio. As implementações das interfaces são substituíveis sem mudar o caso de uso. Os testes comprovam isso ao trocar `INotificador` e os repositórios por mocks do Moq.
- **I (segregação de interfaces).** O repositório da OS foi dividido entre escrita e consultas, e quem altera a OS não enxerga a paginação. O `INotificador` tem um único método, `NotificarMudancaDeStatus`, que recebe uma `NotificacaoDeStatus` (e-mail, Número da OS, origem, destino, momento e orçamento opcional). O antigo método de envio de orçamento deixou de existir.
- **D (inversão de dependência).** A `Application` depende de abstrações: os repositórios, no `Domain`, e `INotificador`, `IUnidadeDeTrabalho` e `IConsultaDeOrdensDeServico`, na própria `Application`. A `Infrastructure` as implementa, e a `Api` compõe tudo na injeção de dependência. A `Api` depende das classes concretas dos casos de uso de propósito: o caso de uso é a política, não um detalhe, e uma interface para cada um não teria uma segunda implementação (KISS).

### Interpretações dos requisitos

- **IDs de Cliente e Veículo na abertura.** O requisito pede "os dados do cliente, veículo, serviços e peças". A abertura recebe `clienteId` e `veiculoId`, e não os dados completos, porque Cliente e Veículo têm cadastros próprios, com regras de unicidade (CPF/CNPJ, placa). Cadastrá-los dentro da abertura daria ao caso de uso dois motivos para mudar. As peças/insumos entram com Baixa imediata ([ADR 0002](docs/adr/0002-baixa-de-estoque-na-inclusao.md)), e uma falha não deixa OS nem Baixa gravadas.
- **8 status em vez de 6.** A "situação" do requisito é o Status da OS. Os seis status do enunciado estão todos presentes ("Diagnóstico" é Em diagnóstico e "Execução" é Em execução), mais Aprovada e Cancelada, pelos motivos descritos em [Status da OS](#status-da-os). Na listagem, Aprovada entra entre Em execução e Aguardando aprovação, porque é trabalho pronto para começar.
- **Finalizada visível só com filtro explícito.** A exclusão é lógica: nada é apagado, a listagem padrão só filtra. Além de Finalizada e Entregue, ela esconde Cancelada, que também não exige mais trabalho. O Atendente lista as Finalizadas com `status=Finalizada` para registrar a entrega, e qualquer status pode ser filtrado.
- **Endpoint único de decisão.** O requisito fala em um "endpoint para receber notificações externas de aprovação ou recusa". Seguimos a leitura literal: um único `POST /api/publico/orcamentos/{token}/decisao`, e as rotas `/aprovar` e `/recusar` deixaram de existir. A equipe preferia as duas rotas, mas optou pela aderência ao texto. O Token de aprovação continua sendo a única credencial: o Número da OS e o documento não autorizam decidir.
- **E-mail de melhor esforço.** "Atualização de status via e-mail" foi lida como a Notificação de status ao Cliente a cada mudança, enviada por SMTP com MailKit. Não há mudança de status por e-mail recebido: a única entrada externa é a Decisão do orçamento, cujo link vai no e-mail de Aguardando aprovação. O envio acontece depois do commit ([ADR 0006](docs/adr/0006-politicas-sincronas-e-eventos-de-dominio.md)) e é de melhor esforço ([ADR 0010](docs/adr/0010-notificacao-de-status-por-email-de-melhor-esforco.md)): se o SMTP falhar, a falha vai para o log e a mudança de status responde com sucesso.

### Estratégia de testes

- **Integração, na API HTTP.** É a base principal: `ApiFactory`, autenticação de teste e PostgreSQL real via Testcontainers. Cobre a listagem (filtro padrão, ordem, paginação, `total` e desempate estável entre páginas), a abertura com peças/insumos, o endpoint de decisão e a Notificação de status. O `ApiFactory` registra um `Mock<INotificador>`, que verifica destinatário, origem, destino e orçamento, e que uma falha do notificador não altera a resposta. Os testes não usam Mailpit nem outro container além do PostgreSQL.
- **Unitários.** Cobrem o agregado da OS (`StatusDaOsAlterado` em toda transição, inclusive abertura e cancelamento; peças/insumos na abertura; `RegrasDoStatusOs`) e os casos de uso com Moq (`AbrirOs`, `NotificarClienteDaMudancaDeStatus`, decisão do orçamento, cancelamento, diagnóstico), sem repositórios em memória.
- **Fora dos testes automatizados.** O envio SMTP do `NotificadorPorEmail`, por decisão explícita (a composição da mensagem é testada em `MensagemDeMudancaDeStatusTests`), e as interfaces web e iOS.
- **Gate de cobertura.** O CI (`.github/workflows/ci.yml`) roda `scripts/cobertura.sh` e falha abaixo de 80% de linhas em Domain + Application. Os números da última execução estão em [Testes e cobertura](#testes-e-cobertura).

## Tecnologias

- C# / .NET 10, ASP.NET Core Minimal API, OpenAPI nativo + Swagger UI
- Entity Framework Core + Npgsql, PostgreSQL 17
- Keycloak 26.2 (JWT, papéis `atendente`, `mecanico`, `gerente`)
- xUnit, Moq, Testcontainers (PostgreSQL), `WebApplicationFactory`
- coverlet + ReportGenerator (cobertura)
- App iOS: SwiftUI (Swift 6), sem dependências de terceiros
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

Sobe PostgreSQL (porta 5432), Keycloak (porta 8080, com o realm `oficina` importado automaticamente), Mailpit (SMTP na porta 1025 e interface web na porta **8025**), a API (porta **8081**) e a interface web (porta **3000**). A API só inicia depois que os dois ficam saudáveis (o Keycloak leva cerca de um minuto na primeira vez). Ao iniciar, a API aplica as migrations e carrega dados de demonstração de forma idempotente.

Verificação rápida:

```bash
curl -i http://localhost:8081/saude
```

Para derrubar o ambiente (e apagar o volume do banco): `docker compose down -v`.

### Executando a API fora do Docker

Suba só as dependências e rode a API (porta 5233, com a carga de demonstração ligada pelo `appsettings.Development.json`):

```bash
docker compose up -d postgres keycloak mailpit
dotnet run --project src/Oficina.Api
```

Neste modo o botão Authorize do Swagger não funciona (o redirecionamento OAuth do realm só está cadastrado para as portas 8081 e 5000); obtenha o token pela linha de comando, como descrito abaixo.

### E-mails de Notificação de status

No docker-compose, a API entrega os e-mails ao **Mailpit**, que os captura sem enviar nada para fora. Abra <http://localhost:8025> para ver cada Notificação de status em texto puro: o assunto segue o formato `<Número da OS>: <status legível>` (ex.: `OS-2026-00001: Em execução`) e, em Aguardando aprovação, o corpo lista os itens e o total e traz o link `http://localhost:3000/portal/consulta?numero=<Número da OS>&token=<Token de aprovação>`. O CPF/CNPJ nunca vai no link: o Cliente o informa na consulta. Rodando a API fora do Docker, o padrão do `appsettings.json` (`localhost:1025`) aponta para o mesmo Mailpit.

O envio é de melhor esforço (ADR 0010): se o SMTP falhar ou passar do timeout, a falha vai para o log da API e a mudança de status responde com sucesso.

Para usar um SMTP real (Brevo, Gmail com senha de app etc.), troque a configuração da seção `Email` por variáveis de ambiente:

| Variável | Padrão | Uso |
|---|---|---|
| `Email__Host` | `localhost` (`mailpit` no compose) | servidor SMTP |
| `Email__Porta` | `1025` | porta SMTP (ex.: 587 com STARTTLS, 465 com SSL) |
| `Email__Usuario` | vazio | login SMTP; vazio desliga a autenticação |
| `Email__Senha` | (sem padrão) | senha SMTP; só por variável de ambiente (Secret no Kubernetes), nunca versionada |
| `Email__Remetente` | `Oficina <nao-responda@oficina.local>` | endereço do remetente |
| `Email__Tls` | `None` | `None`, `StartTls`, `SslOnConnect`, `StartTlsWhenAvailable` ou `Auto` |
| `Email__TimeoutEmSegundos` | `5` | limite de conexão e envio |
| `Email__UrlDoPortal` | `http://localhost:3000` | base do link da consulta pública |

Exemplo com o Brevo: `Email__Host=smtp-relay.brevo.com`, `Email__Porta=587`, `Email__Tls=StartTls`, `Email__Usuario=<login SMTP>`, `Email__Senha=<chave SMTP>` e `Email__Remetente=<remetente verificado>`.

## Interface web

A interface web (`web/`, Next.js) atende os funcionários e é a forma recomendada de usar a oficina no dia a dia. Com o ambiente de pé, acesse <http://localhost:3000>: o navegador é levado ao login do Keycloak e volta para a área administrativa com o nome e o papel do funcionário no topo, um menu montado para o papel e a tela inicial do papel (Balcão para o Atendente, Fila de execução para o Mecânico, Serviços para o Gerente). O Mecânico também vê no menu "Ordens de serviço" (lista com filtro por Status) e continua podendo abrir uma OS pelo link em "Localizar OS". Use os usuários de demonstração da seção abaixo. O botão **Sair** encerra a sessão da interface e a do Keycloak.

A interface funciona como BFF ([ADR 0007](docs/adr/0007-interface-web-como-bff.md)): o login usa authorization code + PKCE no client confidencial `oficina-web`, o JWT fica num cookie criptografado da sessão do servidor e toda chamada à API parte do servidor Next.js. O navegador nunca vê o token, e a API continua sem CORS. O secret do client `oficina-web` (`oficina-web-secret-somente-dev`) e o `AUTH_SECRET` versionados no realm, no `docker-compose.yml` e em `web/.env.development` são **exclusivos do ambiente de demonstração local**.

### Portal do Cliente

O Cliente não tem conta nem login. As páginas do portal são públicas e chamam só os endpoints `/api/publico/**`:

- <http://localhost:3000/portal/consulta>: consulta da OS pelo Número da OS (`OS-AAAA-NNNNN`) e pelo CPF/CNPJ, com o Status, o Histórico de status e o orçamento vigente.
- <http://localhost:3000/portal/aprovacao>: o Cliente cola o Token de aprovação e é levado a `/portal/aprovacao/{token}`.
- `http://localhost:3000/portal/aprovacao/{token}`: aprova ou recusa (com motivo opcional) a Versão do orçamento daquele token.

Ao concluir o diagnóstico, a OS passa a Aguardando aprovação e o Cliente recebe o e-mail com o orçamento. Numa demonstração, abra o e-mail mais recente da OS no Mailpit (<http://localhost:8025>) e siga o link `/portal/consulta?numero=...&token=...`: o portal já preenche o Número da OS, pede o CPF/CNPJ e mostra Aprovar e Recusar abaixo do orçamento. Cada conclusão do diagnóstico gera um token novo, então use o link do e-mail mais recente. O token também pode ser colado em `/portal/aprovacao`.

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

## App iOS

O app iOS (`ios/`, SwiftUI) é feito para o **Mecânico** (e para o Gerente, que pode tudo), que trabalha ao lado do Veículo e não no computador do balcão. Ele tem duas abas: **Fila de execução** (OS Aprovadas) e **Ordens de serviço** (OS que ainda exigem trabalho, sem Finalizadas, Entregues e Canceladas, carregadas por página, com filtro por Status e busca pelo Número da OS, como `OS-2026-00001`). Ambas levam à página da OS, onde o Mecânico faz o diagnóstico (Serviços e Peças/insumos, conclusão) e a execução (início e finalização). O Atendente que entrar vê uma tela dizendo que o app é do Mecânico e que deve usar a interface web.

O app chama a API diretamente, com o token no Keychain ([ADR 0009](docs/adr/0009-app-ios-chama-a-api-com-token-no-keychain.md)): o login usa authorization code + PKCE na página do Keycloak, pelo client público `oficina-ios` (redirect `oficina://autenticacao`), e o app renova o token sozinho e encerra a sessão no Keycloak ao sair.

### Requisitos

- macOS com Xcode 26 e o Simulador do iOS (alvo mínimo iOS 18, só iPhone).
- Docker com Docker Compose v2, para o ambiente que o app consome.

### Passo a passo

1. Suba o ambiente na raiz do repositório e espere o Keycloak e a API ficarem prontos:

   ```bash
   docker compose up --build
   ```

2. Abra `ios/Oficina.xcodeproj` no Xcode.
3. Escolha um iPhone no Simulador e rode o app (Cmd+R).
4. Toque em entrar e faça login na página do Keycloak com `mecanico` / `mecanico123` ou `gerente` / `gerente123` (veja [Autenticação e usuários de demonstração](#autenticação-e-usuários-de-demonstração)).

A API fica em `http://localhost:8081` e o Keycloak em `http://localhost:8080/realms/oficina`, configurados num único lugar do app (`ios/Oficina/Configuracao.swift`).

### Limitações conhecidas

- **Só roda no Simulador do iOS.** O Simulador alcança o `localhost` do Mac, e o issuer dos tokens é `localhost`; o ATS libera HTTP sem TLS apenas para `localhost`. iPhone físico, TestFlight e App Store exigiriam outro hostname no Keycloak, TLS e conta de desenvolvedor.
- **As listas não mostram a placa.** O resumo da API não traz o Veículo, então a OS é identificada pelo Número da OS e o Veículo é conferido na página da OS.
- **Só as ações do Mecânico.** Balcão, Clientes, Veículos, abertura, cancelamento e entrega (Atendente), catálogo, estoque e tempo médio (Gerente) e o Portal do Cliente continuam na interface web.
- Sem modo offline, cache ou notificações, e sem testes automatizados nem CI para o app.

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
| `POST /api/administrativo/ordens-de-servico` (abrir) | Atendente |
| `GET /api/administrativo/ordens-de-servico` (listagem paginada: `status`, `pagina`, `tamanho`) | Atendente ou Mecânico |
| `GET /api/administrativo/ordens-de-servico/{id}` (detalhe) | Atendente ou Mecânico |
| `POST/DELETE .../ordens-de-servico/{id}/servicos` | Atendente ou Mecânico |
| `POST .../{id}/diagnostico/iniciar`, `.../diagnostico/concluir` | Mecânico |
| `POST/DELETE .../{id}/pecas-insumos` | Mecânico |
| `GET .../ordens-de-servico/fila-de-execucao`, `POST .../{id}/execucao/iniciar`, `.../execucao/finalizar` | Mecânico |
| `POST .../{id}/entrega`, `POST .../{id}/cancelar` | Atendente |
| `GET .../ordens-de-servico/tempo-medio-de-execucao` (média geral e por Combinação de serviços) | Gerente |
| `POST /api/publico/ordens-de-servico/consulta` (`numero` + `documento`) | público |
| `POST /api/publico/orcamentos/{token}/decisao` (Decisão do orçamento: `decisao` = `Aprovar` ou `Recusar`, `motivo` opcional) | público (token) |

O Gerente é aceito em todos os endpoints administrativos. A listagem de OS oculta Finalizada, Entregue e Cancelada por padrão, ordena por prioridade (Em execução, Aprovada, Aguardando aprovação, Em diagnóstico, Recebida), da mais antiga para a mais nova dentro do status, e responde `{ itens, pagina, tamanho, total }` (veja [Fase 2](#fase-2)).

## Testes e cobertura

```bash
dotnet test Oficina.sln
```

Os testes de integração sobem um PostgreSQL real via Testcontainers (Docker em execução) e usam um handler de autenticação de teste: o Keycloak não é necessário.

- **Unitários** (`tests/Oficina.UnitTests`): casos de uso com repositórios e notificador como mocks Moq, objetos de valor e regras do domínio.
- **Integração** (`tests/Oficina.IntegrationTests`): API HTTP com PostgreSQL real e migrations aplicadas, cobrindo o ciclo completo da OS, abertura com peças/insumos, listagem paginada, Decisão do orçamento, Notificação de status (com o notificador como mock), recusa, cancelamento com estorno, falta de saldo, OS em andamento duplicada, token reutilizado, consulta pública, 401/403 e inativação.

Cobertura (coverlet + ReportGenerator) com gate de 80% em Domain + Application:

```bash
bash scripts/cobertura.sh
```

O script roda a suíte, gera o relatório em `coverage/` (abra `coverage/index.html`) e falha se a cobertura de linhas ficar abaixo de 80% (ajustável por `LIMITE_COBERTURA`). Na última execução (commit `5a84a86`, 333 testes unitários e 139 de integração, todos aprovados): **98,8% de linhas em Domain + Application** (Domain 97,9%, Application 99,6%). Infrastructure e Api são cobertas pelos testes de integração, sem meta percentual.

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
│   ├── Oficina.Infrastructure/  EF Core, repositórios, migrations, notificador por e-mail (MailKit)
│   └── Oficina.Api/             endpoints, autenticação Keycloak, OpenAPI, tratamento de erros
├── tests/
│   ├── Oficina.UnitTests/       testes unitários (Moq)
│   └── Oficina.IntegrationTests/ testes de integração (Testcontainers)
├── docs/
│   ├── ddd/                     Event Storming, diagramas e linguagem ubíqua
│   └── seguranca/               relatório de vulnerabilidades e relatórios brutos
├── web/                         interface web (Next.js, BFF)
├── ios/                         app iOS do Mecânico (SwiftUI)
├── keycloak/oficina-realm.json  realm importado (papéis, usuários de demonstração, clients do Swagger, da interface web e do app iOS)
├── scripts/cobertura.sh         testes + gate de cobertura
├── .github/workflows/           ci.yml e vulnerabilidades.yml
├── Dockerfile                   build multi-stage da API
├── docker-compose.yml           PostgreSQL + Keycloak + Mailpit + API + interface web
├── GLOSSARY.md                  glossário do domínio
└── Oficina.sln
```

## Documentação DDD

- [Linguagem ubíqua e relação termo → código](docs/ddd/linguagem-ubiqua.md)
- [Event Storming](docs/ddd/event-storming.md) (quadro em [PDF](docs/ddd/event-storming.pdf) e [imagem](docs/ddd/event-storming.png)) e [alterações aplicadas ao quadro](docs/ddd/alteracoes-event-storming.md)
- [Diagramas](docs/ddd/diagramas.pdf): agregados, bounded contexts e máquina de status da OS
- [Glossário](GLOSSARY.md)
- [Decisões arquiteturais (ADRs)](docs/adr/)
- Quadro do Event Storming: versionado no próprio repositório, em [`docs/ddd/event-storming.pdf`](docs/ddd/event-storming.pdf) e [`docs/ddd/event-storming.png`](docs/ddd/event-storming.png)

## Vídeo da entrega

<https://youtu.be/4QP7ZSufk6w>
