# Notificação de status por e-mail, de melhor esforço, após o commit

Toda mudança de Status da OS gera uma Notificação de status por e-mail ao Cliente da OS. O envio usa SMTP via MailKit e acontece no manipulador do evento de mudança de status, despachado **depois** do commit (ADR 0006), dentro da mesma requisição. O envio é de **melhor esforço**: usa timeout curto e, se o SMTP falhar, a falha vai para o log e a requisição responde com sucesso, porque a mudança de status já está gravada. Aceitamos perder um e-mail numa queda do SMTP em troca de não ter tabela de saída, worker nem coordenação entre réplicas.

## Considered Options

- **Outbox com worker em segundo plano**: grava a notificação na mesma transação e reenvia até conseguir, sem perder e-mails. Com o HPA haveria várias réplicas, então exigiria travar a leitura (`FOR UPDATE SKIP LOCKED`). É o próximo passo se e-mails perdidos virarem problema.
- **Deixar a exceção subir**: a API responderia 500 com a mudança já gravada, e uma nova tentativa do cliente levaria a um conflito.

## Consequences

- Um Cliente pode deixar de receber uma Notificação de status sem que ninguém perceba, a não ser pelo log.
- O tempo de resposta das ações que mudam status inclui o envio do e-mail, limitado pelo timeout.
