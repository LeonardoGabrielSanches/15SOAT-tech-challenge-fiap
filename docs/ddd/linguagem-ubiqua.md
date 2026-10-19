# Linguagem ubíqua

Derivada do [`GLOSSARY.md`](../../GLOSSARY.md), que é a fonte dos termos e das definições. Este documento relaciona cada termo ao tipo ou método que o implementa no código entregue. Caminhos relativos a `src/`. Os conceitos do domínio estão em português sem acentos; os termos técnicos, em inglês.

## Bounded contexts no código

| Contexto | Onde está | Observação |
|---|---|---|
| Atendimento (Core) | `Oficina.Domain/Atendimento`, `Oficina.Application/Atendimento` | Cliente, Veículo, OS, orçamento, histórico |
| Catálogo e Estoque (Supporting) | `Oficina.Domain/CatalogoEstoque` (Serviço, Dinheiro), `Oficina.Domain/Estoque` (Peça/insumo, Quantidade) | O Estoque ficou em pasta própria no domínio, mas pertence ao mesmo contexto |
| Identidade e Acesso (Generic) | Keycloak (`keycloak/oficina-realm.json`) e `Oficina.Api/Autenticacao` | Externo ao código de negócio; a API só valida o JWT e checa o papel |

## Termo → código

### Atendimento

| Termo | Tipo / método | Arquivo |
|---|---|---|
| Cliente | `Cliente` (raiz de agregado); `Cliente.Cadastrar`, `AtualizarDados`, `Inativar` | `Oficina.Domain/Atendimento/Entities/Cliente.cs` |
| CPF ou CNPJ do Cliente | `Documento` (escolhe entre `Cpf` e `Cnpj`, numérico e alfanumérico); `TipoDocumento` | `Entities/ValueObjects/Documento.cs`, `Cpf.cs`, `Cnpj.cs` |
| E-mail | `Email` (objeto de valor) | `Entities/ValueObjects/Email.cs` |
| Veículo | `Veiculo` (raiz de agregado); `Veiculo.Cadastrar`, `AtualizarDados`, `Inativar` | `Entities/Veiculo.cs` |
| Placa (padrão antigo e Mercosul) | `Placa` (normalizada) | `Entities/ValueObjects/Placa.cs` |
| Ano do Veículo | `AnoFabricacao` (de 1900 até o ano atual + 1) | `Entities/ValueObjects/AnoFabricacao.cs` |
| Proprietário atual | `Veiculo.ProprietarioId`; `Veiculo.Transferir`; caso de uso `TransfereVeiculo` | `Entities/Veiculo.cs`, `Application/Atendimento/UseCases/CasosDeUsoDeVeiculo.cs` |
| Inativo | `Ativo = false` por `Inativar()` em `Cliente`, `Veiculo`, `Servico`, `PecaInsumo`; `GarantirAtivo()` rejeita o uso em novas operações | entidades correspondentes |

### Ordem de Serviço

| Termo | Tipo / método | Arquivo |
|---|---|---|
| Ordem de Serviço (OS) | `OrdemDeServico` (raiz do agregado do fluxo); `OrdemDeServico.Abrir` | `Entities/OrdemDeServico.cs` |
| Número da OS | `NumeroOs` (`OS-AAAA-NNNNN`); gerado por `IGeradorNumeroOs`, implementado com sequência por ano no PostgreSQL (`GeradorNumeroOs`) | `Entities/ValueObjects/NumeroOs.cs`, `Interfaces/IGeradorNumeroOs.cs`, `Oficina.Infrastructure/Persistencia/Atendimento/GeradorNumeroOs.cs` |
| Status da OS | enum `StatusOs` (`Recebida`, `EmDiagnostico`, `AguardandoAprovacao`, `Aprovada`, `EmExecucao`, `Finalizada`, `Entregue`, `Cancelada`) | `Entities/Enums/StatusOs.cs` |
| Histórico de status | `EntradaDoHistoricoDeStatus` (origem, destino, momento, autor); `OrdemDeServico.Historico` | `Entities/OrdemDeServico.cs` |
| OS em andamento (no máximo uma por Veículo) | `IOrdemDeServicoRepository.ExisteEmAndamentoDoVeiculo`, verificado em `AbrirOs`; índice único parcial `ux_ordens_de_servico_veiculo_em_andamento` como segunda barreira | `Interfaces/IOrdemDeServicoRepository.cs`, `UseCases/CasosDeUsoDeOrdemDeServico.cs`, `OrdemDeServicoConfiguration.cs` |
| Recebida | `StatusOs.Recebida`; caso de uso `AbrirOs`, evento `OsAberta` | |
| Serviço da OS | `ServicoDaOs`; `OrdemDeServico.IncluirServico`, `RemoverServico`; casos de uso `IncluirServicoNaOs`, `RemoverServicoDaOs` | `Entities/OrdemDeServico.cs` |
| Diagnóstico | `OrdemDeServico.IniciarDiagnostico`, `ConcluirDiagnostico`; casos de uso `IniciarDiagnostico`, `ConcluirDiagnostico`; eventos `DiagnosticoIniciado`, `DiagnosticoConcluido` | |
| Aprovada | `StatusOs.Aprovada`; `OrdemDeServico.AprovarOrcamento`; fila `ListarFilaDeExecucao` | |
| Em execução / Finalizada | `OrdemDeServico.IniciarExecucao`, `FinalizarExecucao`; casos de uso `IniciarExecucao`, `FinalizarExecucao` | `UseCases/CasosDeUsoDeExecucao.cs` |
| Entregue | `OrdemDeServico.RegistrarEntrega`; caso de uso `RegistrarEntrega` | `UseCases/CasosDeUsoDeExecucao.cs` |
| Cancelada | `OrdemDeServico.Cancelar` (motivo obrigatório; estorna todas as Peças/insumos); caso de uso `CancelarOs`; evento `OsCancelada` | |
| Tempo de execução | intervalo corrido entre `EmExecucao` e `Finalizada` no Histórico de status, medido para a OS inteira; `TempoDeExecucaoDaOs` lido por `IOrdemDeServicoRepository.ObterTemposDeExecucao` | `Interfaces/IOrdemDeServicoRepository.cs`, `OrdemDeServicoRepository.cs` |
| Combinação de serviços | `CombinacaoDeServicos`: conjunto exato de `ServicoDaOs` da OS (sem ordem, sem peças/insumos); `ConsultarTempoMedioDeExecucao` agrupa os tempos por ela e devolve `TempoMedioDaCombinacaoDto` | `Entities/ValueObjects/CombinacaoDeServicos.cs`, `UseCases/CasosDeUsoDeExecucao.cs` |
| Consulta do Cliente | `ConsultarOsPublica` (Número da OS + CPF/CNPJ) | `UseCases/ConsultarOsPublica.cs` |

### Orçamento

| Termo | Tipo / método | Arquivo |
|---|---|---|
| Orçamento | conjunto de `VersaoDoOrcamento` da OS (`OrdemDeServico.Versoes`) | `Entities/VersaoDoOrcamento.cs` |
| Versão do orçamento | `VersaoDoOrcamento` (itens `ItemDaVersao` com preços congelados, total, situação `SituacaoDaVersao`); gerada em `OrdemDeServico.ConcluirDiagnostico` | `Entities/VersaoDoOrcamento.cs` |
| Token de aprovação | `VersaoDoOrcamento.Token` (32 bytes aleatórios, uso único, sem expiração por tempo); validado em `OrdemDeServico.AprovarOrcamento` e `RecusarOrcamento` | `Entities/OrdemDeServico.cs` |
| Aprovar / recusar orçamento | casos de uso `AprovarOrcamento` e `RecusarOrcamento`; eventos `OrcamentoAprovado` e `OrcamentoRecusado`; motivo em `MotivoDaUltimaRecusa` | `UseCases/CasosDeUsoDeAprovacao.cs` |
| Envio do orçamento | handler `EnviarOrcamentoAoConcluirDiagnostico` do evento `DiagnosticoConcluido`, via `INotificador` (implementação simulada `NotificadorSimulado`) | `Application/Common/INotificador.cs`, `Oficina.Infrastructure/NotificadorSimulado.cs` |

### Catálogo e estoque

| Termo | Tipo / método | Arquivo |
|---|---|---|
| Serviço | `Servico` (raiz de agregado); `Servico.Cadastrar`, `Atualizar`, `Inativar` | `Oficina.Domain/CatalogoEstoque/Entities/Servico.cs` |
| Preço | `Dinheiro` (BRL, objeto de valor) | `CatalogoEstoque/Entities/ValueObjects/Dinheiro.cs` |
| Peça/insumo | `PecaInsumo` (raiz de agregado, inclui o saldo); `PecaInsumo.Criar`, `Atualizar`, `Inativar` | `Oficina.Domain/Estoque/PecaInsumo.cs` |
| Peça / Insumo | `TipoPecaInsumo.Peca` (quantidade inteira) / `TipoPecaInsumo.Insumo` (fracionária); `Quantidade.Criar(valor, tipo)` valida | `Estoque/TipoPecaInsumo.cs`, `Estoque/Quantidade.cs` |
| Baixa | `PecaInsumo.Baixar`, disparada por `OrdemDeServico.IncluirPecaInsumo` (caso de uso `IncluirPecaInsumoNaOs`); evento `ItemIncluido` | `Estoque/PecaInsumo.cs` |
| Estorno | `PecaInsumo.Estornar`, por `OrdemDeServico.RemoverPecaInsumo` (total ou parcial) e por `Cancelar` | `Estoque/PecaInsumo.cs` |
| Entrada de mercadoria | `PecaInsumo.RegistrarEntrada`; caso de uso `RegistrarEntradaDeMercadoriaCasoDeUso` | `Estoque/PecaInsumo.cs`, `Application/Estoque/PecasInsumos.cs` |

### Pessoas

| Termo | Implementação |
|---|---|
| Atendente | papel `atendente` do Keycloak; política `Politicas.Atendente` |
| Mecânico | papel `mecanico`; política `Politicas.Mecanico` |
| Gerente | papel `gerente`; passa em todas as políticas (`Oficina.Api/Autenticacao/Politicas.cs`, `AutenticacaoKeycloak.cs`) |
| Cliente | não é usuário do sistema: acessa os endpoints públicos `/api/publico/**` por Número da OS + documento ou por token |

## Eventos de domínio no código

O Event Storming ([`event-storming.md`](event-storming.md)) descreve todos os eventos do fluxo. No código, as entidades registram os eventos abaixo (`Oficina.Domain/Atendimento/Events`): `OsAberta`, `DiagnosticoIniciado`, `ItemIncluido`, `DiagnosticoConcluido`, `OrcamentoAprovado`, `OrcamentoRecusado` e `OsCancelada`. As demais mudanças (início e fim da execução, entrega, estorno) são registradas como entradas do Histórico de status e como efeitos síncronos no caso de uso, sem evento de domínio próprio. Hoje só `DiagnosticoConcluido` tem handler (despachado por `DespachanteDeEventos` após o commit).

## Por que os status Aprovada e Cancelada

O enunciado exige seis status (Recebida, Em diagnóstico, Aguardando aprovação, Em execução, Finalizada, Entregue) e não proíbe outros. Dois foram acrescentados:

- **Aprovada**: a aprovação do Cliente (feita por token, a qualquer hora) é um fato diferente do início do trabalho pelo Mecânico. Sem este status, a aprovação teria de levar direto a Em execução, o que falsearia o Tempo de execução (o relógio correria antes de alguém tocar no Veículo) e eliminaria a fila de OS aprovadas, pela qual o Mecânico escolhe o que executar a seguir.
- **Cancelada**: uma OS abandonada antes da execução precisa de um fim explícito. Sem ele, ficaria eternamente "em andamento" e bloquearia a abertura de outra OS para o mesmo Veículo. O cancelamento exige motivo, estorna as Peças/insumos baixadas e invalida o token pendente.

Os seis status obrigatórios mantêm nome e significado.

## Por que o tempo é medido por OS e comparado por Combinação de serviços

O Mecânico registra o início e a finalização da execução da OS, e não de cada serviço. Por isso o Tempo de execução é da OS inteira e em tempo corrido: noites, fins de semana, esperas e OS executadas em paralelo pelo mesmo Mecânico entram no intervalo. O tempo médio é recortado pela Combinação de serviços exata, e não pelas "OS que contêm o Serviço X", porque esse recorte atribuía a um serviço o tempo dos outros serviços da mesma OS: uma troca de óleo de 15 min parecia levar 35 min quando a média incluía uma OS com troca de óleo e troca de pneu (55 min). Uma OS de serviço único já mostra o tempo daquele serviço, sem estimativa. Ver [ADR 0008](../adr/0008-tempo-de-execucao-por-os-recortado-por-combinacao.md).

## Fora do MVP

- Reparos adicionais durante a execução (sem transição de Em execução para Aguardando aprovação).
- Pagamento e qualquer condição financeira para a entrega.
- Envio real de notificações e notificação de OS Finalizada.
- Extrato de movimentações de estoque, alerta de estoque mínimo e ajuste de inventário.
- Descontos no orçamento.
- Login ou conta de Cliente.
- Cancelamento de OS Aprovada, Em execução ou posterior.
- Tempo de execução por Serviço individual (sem apontamento por serviço; o tempo é comparado por Combinação de serviços).
- Gestão de usuários pela API.
