# Baixa de estoque na inclusão da peça/insumo na OS

A baixa acontece quando o Mecânico inclui a peça/insumo na OS durante o diagnóstico, e não no início da execução. Assim o saldo mostra o que ainda está disponível para novas OS, e a regra "não se orça peça que não existe" vale sem um conceito separado de reserva. O estorno acontece quando a peça/insumo é removida da OS ou quando a OS é cancelada. A recusa do orçamento, sozinha, não faz estorno: a OS volta para "Em diagnóstico" e o Mecânico decide o que remover.

## Considered Options

- **Reserva na inclusão e baixa no início da execução**: modela melhor o estoque físico, mas cria dois saldos (físico e disponível) e mais transições.
- **Só validar na inclusão e dar baixa na execução**: mais simples, mas outra OS pode consumir a peça entre o orçamento e a execução, e o orçamento aprovado fica impossível de cumprir.
