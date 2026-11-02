# Tempo de execução medido por OS e recortado por Combinação de serviços

O tempo de execução é medido para a OS inteira, do início à finalização da execução e em tempo corrido, porque o Mecânico não registra início e fim de cada serviço. O tempo médio é recortado pela Combinação de serviços exata, e não pelas "OS que contêm o Serviço X". O recorte antigo atribuía a um serviço o tempo dos outros serviços da mesma OS: uma troca de óleo de 15 min parecia levar 35 min quando a média incluía uma OS com troca de óleo e troca de pneu (55 min). Uma OS de serviço único já mostra o tempo daquele serviço, sem nenhuma estimativa. Lemos o requisito "tempo médio de execução dos serviços" como os serviços da oficina, medidos por OS.

## Considered Options

- **Apontamento por serviço pelo Mecânico**: é a medida exata, mas exige marcar início e fim de cada serviço, o que não acontece no dia a dia da oficina.
- **Agrupar pela quantidade de serviços**: é simples, mas mistura combinações muito diferentes ("óleo + alinhamento" e "motor + câmbio").
- **Rateio do tempo da OS entre os serviços** (partes iguais, por preço ou por tempo estimado no catálogo): inventa números ou exige manter estimativas no catálogo.
- **Estimativa por regressão sobre os totais das OS**: deduz o tempo de cada serviço, mas precisa de volume de dados e é difícil de explicar.

## Consequences

- Sem apontamento, o histórico nunca permitirá medir o tempo de cada serviço retroativamente.
- O tempo corrido inclui noites, fins de semana, esperas e OS executadas em paralelo pelo mesmo Mecânico.
- Combinações raras aparecem com poucas OS; a quantidade de OS é sempre exibida junto da média.
