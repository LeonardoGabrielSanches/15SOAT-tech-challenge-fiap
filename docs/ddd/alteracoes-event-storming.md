# Alterações a refletir no quadro de Event Storming

**Status: aplicado.** Todas as mudanças abaixo (A1–A20, B1, C1–C2, Bounded Contexts e hotspots fora do MVP) estão refletidas no quadro versão 2 (`event-storming.png`, `event-storming.pdf`, fonte em `event-storming.html`) e na transcrição `event-storming.md`. A foto anterior foi mantida como `event-storming-v1.jpg`.

Mudanças decididas após a primeira versão do quadro (`event-storming-v1.jpg`).

## 1. Fluxo da Ordem de Serviço

| # | Onde no quadro | Mudança | Motivo |
|---|---|---|---|
| A1 | 1.3 e 1.4 | Fundir "Cria a ordem de serviço" e "Incluí serviços solicitados" num único comando **Abre a OS** (Atendente), evento **OS aberta**. Remover a política "Status muda para Criada"; a política passa a ser "Status da ordem muda para **Recebida**". | O status "Criada" não existe no enunciado; uma OS sem serviço não tem significado. |
| A2 | 1.4 / 1.6 | O comando de inclusão de serviços passa a ter dois atores: **Atendente** (na abertura) e **Mecânico** (no diagnóstico). Adicionar o comando **Remove serviço da OS** (Mecânico). | O diagnóstico pode revelar serviços que o cliente não pediu. |
| A3 | Entre 1.6 e 1.7 | Novo comando **Conclui diagnóstico** (Mecânico), evento **Diagnóstico concluído**. Ele dispara "Gera o orçamento". | Gerar orçamento a cada peça incluída criava várias versões e notificações, e uma OS sem peças nunca gerava orçamento. |
| A4 | 1.7 políticas | Trocar "Todo orçamento é gerado imediatamente após a inclusão das peças e insumos" por "Todo orçamento é gerado ao concluir o diagnóstico". Adicionar: "A versão do orçamento congela os preços do catálogo". | Ver A3. O valor acordado com o cliente é o da versão, não o do catálogo atual. |
| A5 | 1.7 eventos | Renomear "Orçamento recebido pelo cliente" para **Orçamento enviado ao cliente**. | O sistema não sabe se o cliente recebeu; sabe apenas que enviou. |
| A6 | 1.7 sistema externo | Anotar que o **Sistema de notificação** é simulado no MVP (registra a mensagem e o token). | Não há integração real de envio no escopo. |
| A7 | 1.8 políticas | Detalhar: "Cada versão do orçamento tem um token de uso único; gerar nova versão invalida o token anterior". | Evita aprovar uma versão antiga. |
| A8 | Novo, após 1.8 | Novo comando **Cancela a OS** (Atendente, com motivo obrigatório), evento **OS cancelada**, políticas "Só é possível cancelar em Recebida, Em diagnóstico ou Aguardando aprovação", "Status da ordem muda para **Cancelada**", "Estornar todas as peças/insumos da OS" e "Invalidar o token da versão pendente". | Sem um estado final de desistência, a OS recusada ficava em "Em diagnóstico" para sempre, com estoque baixado. |
| A9 | 1.9 política | Trocar "Calculado tempo médio de execução" por "Registrado tempo de execução da OS (início → finalização)". O tempo médio é um modelo de leitura. | O tempo é da OS inteira, não de cada serviço. |
| A10 | 1.8 / leitura do cliente | Adicionar modelo de leitura **Acompanhamento da OS (cliente)**, consultado pelo **número da OS** (`OS-AAAA-NNNNN`) + CPF/CNPJ. Mostra o status atual e o histórico de status, sem nomes de funcionários. | Requisito de consulta pelo cliente via API. |
| A11 | Todas as políticas "Status da ordem muda para…" | Acrescentar a política geral "Toda mudança de status é registrada no **histórico de status** (origem, destino, momento, autor)". | Permite saber quanto tempo a OS passou em cada etapa e quem a moveu; é a base do tempo de execução. |
| A12 | 1.4 / 1.6 / 1.7 | Adicionar as políticas: "Itens só podem ser incluídos ou removidos em Em diagnóstico (ou serviços pelo Atendente em Recebida)", "Concluir diagnóstico exige ao menos um serviço" e "Total = Σ preço dos serviços + Σ quantidade × preço das peças/insumos". | Regras de transição explícitas. |
| A13 | 1.10 | Anotar: "Entrega não depende de pagamento; Entregue é status final". | Pagamento fora do MVP. |
| A14 | 2 e 3.1 | Trocar "Cadastra/atualiza" por "Cadastra/atualiza/inativa" e acrescentar a política "Exclusão é sempre lógica (inativação)". Vale também para Cliente e Veículo. | Registros antigos continuam referenciados por OS. |
| A15 | 1.8 "Aprova orçamento" | Adicionar a política "Status da ordem muda para **Aprovada**". Em 1.9 "Inicia a execução", a política "O orçamento deve estar aprovado" vira "A OS deve estar **Aprovada**". O modelo de leitura "Lista de OS aprovadas" passa a ser a fila de trabalho do Mecânico. | Sem esse status, a aprovação não mudava o status, ou o tempo de execução contaria o tempo em que o carro esperou na fila. |
| A16 | 1.3 "Abre a OS" | Adicionar as políticas "Só pode existir uma OS em andamento por veículo" e "Validar CPF/CNPJ (numérico e alfanumérico) e placa (padrão antigo e Mercosul)". | Evita duas OS concorrentes no mesmo veículo; validação exigida no enunciado. |
| A17 | 1.6 / 3.3 / 3.4 | Adicionar as políticas: "Incluir peça/insumo já presente soma à quantidade", "Remoção pode ser parcial, com estorno proporcional", "Peça aceita apenas quantidade inteira; insumo aceita fração" e "Um serviço não se repete na mesma OS". | Regras de quantidade explícitas. |
| A18 | Modelos de leitura | Renomear "Lista de OS criadas" para **Lista de OS**: oculta Entregue e Cancelada por padrão e ordena por status (Em execução, Aprovada, Aguardando aprovação, Em diagnóstico, Recebida, Finalizada) e, dentro do status, pela mais antiga. | Ataca o problema de priorização citado no enunciado. |
| A19 | 1.8 "Recusa orçamento" | Anotar que a recusa aceita um **motivo opcional**, visível para o Mecânico no diagnóstico seguinte. Em A7, completar: "O token não expira por tempo; perde validade ao ser usado, ao gerar nova versão ou ao cancelar a OS". | O Mecânico precisa saber o que ajustar; um token antigo nunca aprova uma versão superada. |
| A20 | 1.3 "Abre a OS" | Adicionar a política "O Cliente da OS deve ser o proprietário atual do Veículo". Em 1.2, adicionar o comando **Transfere veículo** (Atendente), evento **Veículo transferido**. | Garante que quem aprova o orçamento é o dono registrado. |

## 2. Atores

| # | Mudança | Motivo |
|---|---|---|
| B1 | Remover **Sistema** como ator. "Realiza baixa" e "Realiza estorno" passam a ser **políticas** disparadas pelos eventos de inclusão, remoção e cancelamento. | Baixa e estorno não são decisões de alguém; são reações automáticas. |

## 3. Agregados

| # | Mudança | Motivo |
|---|---|---|
| C1 | No fluxo da OS, o único post-it de agregado é **Ordem de Serviço**. "Orçamento", "Serviços" e "Peças e insumos" desse fluxo viram **entidades internas da OS** (versões do orçamento e itens da OS). | Aprovar o orçamento e mudar o status da OS precisam acontecer juntos. Ver [ADR 0003](../adr/0003-os-como-agregado-do-fluxo.md). |
| C2 | Agregados finais: **Cliente**, **Veículo**, **Ordem de Serviço**, **Serviço** (catálogo) e **Peça/insumo** (catálogo + saldo). | — |

## 4. Bounded Contexts

Desenhar no quadro três áreas delimitadas:

| Contexto | Tipo | Conteúdo |
|---|---|---|
| **Atendimento** | Core | Cliente, Veículo, Ordem de Serviço (com orçamento, itens e histórico de status) |
| **Catálogo e Estoque** | Supporting | Serviço, Peça/insumo (com saldo), entrada de mercadoria, baixa e estorno |
| **Identidade e Acesso** | Generic | Keycloak: usuários e papéis Atendente, Mecânico e Gerente |

Relação: Atendimento consulta preços do Catálogo e dispara baixa/estorno no Estoque, na mesma transação ([ADR 0003](../adr/0003-os-como-agregado-do-fluxo.md)). No código, Atendimento e Catálogo e Estoque são módulos do monólito.

## 5. Decisões que precisam ser apresentadas

- **Baixa de estoque na inclusão** — ver [ADR 0002](../adr/0002-baixa-de-estoque-na-inclusao.md).
- **OS como agregado do fluxo, gravada na mesma transação que o saldo** — ver [ADR 0003](../adr/0003-os-como-agregado-do-fluxo.md).
- **Status extras "Aprovada" e "Cancelada"** — o enunciado lista os seis status obrigatórios mas não proíbe outros. "Aprovada" separa a aprovação do cliente do início real da execução; "Cancelada" encerra OS abandonadas.

## 6. Fora do MVP (hotspots documentados)

- **Reparos adicionais durante a execução.** O texto do desafio menciona "autorizar reparos adicionais via aplicativo", mas a lista de funcionalidades obrigatórias não. No MVP não existe transição de "Em execução" de volta para "Aguardando aprovação"; qualquer ajuste de itens só acontece no ciclo de diagnóstico, antes da aprovação.
