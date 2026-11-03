# Oficina — Atendimento e Execução de Serviços

Linguagem da oficina mecânica: do atendimento do cliente à entrega do veículo, passando por diagnóstico, orçamento, execução e controle de peças e insumos. Um único glossário cobre os contextos Atendimento (Cliente, Veículo, OS, Orçamento) e Catálogo e Estoque (Serviço, Peça/insumo), porque a linguagem não diverge entre eles.

## Atendimento

**Cliente**:
Pessoa física ou jurídica dona de veículos atendidos pela oficina, identificada por CPF ou CNPJ.
_Avoid_: Usuário, consumidor

**Veículo**:
Automóvel identificado pela placa, com marca, modelo, ano e um único proprietário atual.
_Avoid_: Carro, automóvel

**Proprietário atual**:
O Cliente a quem o Veículo pertence hoje; pode ser transferido sem alterar o histórico de OS anteriores.
_Avoid_: Dono, titular

## Ordem de Serviço

**Ordem de Serviço (OS)**:
O registro de um atendimento de um Veículo de um Cliente, da recepção à entrega.
_Avoid_: Pedido, chamado, ticket

**Número da OS**:
Identificador amigável da OS no formato `OS-AAAA-NNNNN`, com sequência reiniciada a cada ano, informado ao Cliente para consulta; não é o identificador técnico.
_Avoid_: ID da OS, protocolo

**Histórico de status**:
Registro de cada mudança de Status da OS: origem, destino, momento e quem a fez.
_Avoid_: Log, auditoria, timeline

**Status da OS**:
Etapa em que a OS se encontra: Recebida, Em diagnóstico, Aguardando aprovação, Aprovada, Em execução, Finalizada, Entregue ou Cancelada.
_Avoid_: Estado, fase, situação

**OS em andamento**:
OS que ainda não está Entregue nem Cancelada; um Veículo tem no máximo uma.
_Avoid_: OS aberta, OS ativa

**Recebida**:
Status inicial da OS, já com Cliente, Veículo e ao menos um serviço solicitado.
_Avoid_: Criada, aberta

**Aprovada**:
Status da OS cuja Versão do orçamento mais recente foi aprovada pelo Cliente e que aguarda o Mecânico iniciar a execução.
_Avoid_: Autorizada, liberada

**Fila de execução**:
As OS Aprovadas, ordenadas pela abertura, de onde o Mecânico escolhe a próxima a executar.
_Avoid_: Backlog, fila de trabalho

**Cancelada**:
Status final de uma OS abandonada antes da execução, com motivo obrigatório; devolve ao estoque todas as peças/insumos baixados para ela.

**Entregue**:
Status final da OS em que o Veículo foi devolvido ao Cliente; não depende de pagamento.
_Avoid_: Desistência, encerrada

**Serviço da OS**:
Um Serviço do catálogo incluído numa OS, seja na abertura pelo Atendente, seja no diagnóstico pelo Mecânico.
_Avoid_: Serviço solicitado, serviço diagnosticado

**Diagnóstico**:
Etapa em que o Mecânico avalia o Veículo e ajusta os serviços e peças/insumos da OS; termina com a conclusão do diagnóstico.
_Avoid_: Avaliação, vistoria

**Tempo de execução**:
Intervalo corrido entre o início e a finalização da execução de uma OS, sem descontar noites, fins de semana ou esperas, medido para a OS inteira e não por serviço.
_Avoid_: Duração do serviço, lead time

**Combinação de serviços**:
O conjunto exato de Serviços de uma OS, sem considerar ordem nem peças/insumos; é por ela que o tempo de execução é comparado entre OS.
_Avoid_: Pacote, composição, kit

## Orçamento

**Orçamento**:
Valor proposto ao Cliente para os serviços e peças/insumos de uma OS; cada conclusão do diagnóstico gera uma nova versão.
_Avoid_: Cotação, proposta

**Versão do orçamento**:
Fotografia dos itens e preços de uma OS no momento em que o orçamento é gerado; os preços da versão não mudam depois.
_Avoid_: Revisão

**Token de aprovação**:
Código de uso único, emitido para uma Versão do orçamento, que o Cliente usa para aprovar ou recusar essa versão.
_Avoid_: Senha, link de aprovação

**Decisão do orçamento**:
A resposta do Cliente a uma Versão do orçamento, informada com o Token de aprovação: Aprovar ou Recusar.
_Avoid_: Reprovar, rejeitar, resposta

## Comunicação com o Cliente

**Notificação de status**:
E-mail enviado ao Cliente a cada mudança de Status da OS, inclusive a abertura.
_Avoid_: Alerta, aviso, push

## Catálogo e estoque

**Serviço**:
Trabalho que a oficina oferece no catálogo, com preço (ex.: troca de óleo, alinhamento).
_Avoid_: Mão de obra, tarefa

**Peça/insumo**:
Item físico do catálogo com saldo em estoque; tem um tipo (peça ou insumo) e uma unidade de medida.
_Avoid_: Produto, material, item de estoque

**Peça**:
Peça/insumo contada em unidades (ex.: filtro de óleo).

**Insumo**:
Peça/insumo consumida em quantidade fracionável (ex.: óleo de motor em litros).

**Baixa**:
Redução do saldo de uma Peça/insumo ao ser incluída numa OS.
_Avoid_: Saída, consumo, reserva

**Estorno**:
Devolução ao saldo de uma Peça/insumo que foi baixada para uma OS e deixou de ser usada nela.
_Avoid_: Devolução, cancelamento de baixa

**Inativo**:
Registro retirado de uso: não pode ser escolhido em novas operações, mas continua visível no histórico. É o resultado de toda exclusão.
_Avoid_: Excluído, deletado, removido

**Entrada de mercadoria**:
Aumento do saldo de uma Peça/insumo por compra ou reposição.
_Avoid_: Reposição, compra

## Pessoas

**Atendente**:
Funcionário que identifica Cliente e Veículo, abre a OS e registra a entrega.
_Avoid_: Recepcionista

**Mecânico**:
Funcionário que diagnostica o Veículo, ajusta itens da OS e executa os serviços.
_Avoid_: Técnico

**Gerente**:
Funcionário que mantém o catálogo e o estoque e pode realizar qualquer ação administrativa.
_Avoid_: Administrador
