# Ordem de Serviço como agregado do fluxo, com o saldo na mesma transação

O Event Storming mostrava Orçamento, Serviços e Peças e insumos como agregados separados da OS. Decidimos que a **OS é o único agregado do fluxo**: as versões do orçamento, os tokens de aprovação, os itens e o histórico de status ficam dentro dela. Assim, aprovar uma versão e mudar o status da OS são uma única operação. Incluir ou remover uma peça/insumo também altera o saldo, que é outro agregado. Por isso a OS e a Peça/insumo são gravadas **na mesma transação**, em vez de usar consistência eventual. Isso contraria a regra usual de "um agregado por transação", mas é aceitável num monólito com um único banco, e garante a regra "não se orça peça que não existe".

## Considered Options

- **Orçamento como agregado próprio**: exigiria coordenar duas gravações (orçamento aprovado + status da OS) e tratar falhas parciais.
- **Baixa por evento assíncrono**: respeitaria "um agregado por transação", mas abriria uma janela em que duas OS incluem a última unidade da mesma peça.
