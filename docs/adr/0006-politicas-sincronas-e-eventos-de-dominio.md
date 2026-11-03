# Políticas síncronas no caso de uso; eventos de domínio só após o commit

As políticas do Event Storming que mantêm a consistência são executadas **diretamente pelo caso de uso, na mesma transação**: baixa, estorno e registro no histórico de status. As entidades também registram os eventos de domínio do quadro (`OsAberta`, `DiagnosticoConcluido`, `OrcamentoAprovado`…). Um dispatcher simples os despacha **depois** do `SaveChanges`, e hoje só a mudança de status tem consumidor: a Notificação de status por e-mail ao Cliente (ADR 0010). Assim os eventos existem no código, mas nenhuma regra de consistência depende da entrega de um evento.

## Considered Options

- **Baixa e estorno via handler de evento**: mais próximo do quadro, mas exigiria despachar os eventos antes do commit, dentro da transação, ou aceitar consistência eventual no saldo.
