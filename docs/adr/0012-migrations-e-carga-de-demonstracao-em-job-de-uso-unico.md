# Migrations e carga de demonstração num Job de uso único, fora do startup

A API deixa de aplicar migrations e de executar a Carga de demonstração ao subir. Essas duas tarefas passam a rodar num processo de uso único (o serviço `migrator` no Compose e um Job no Kubernetes), que usa a imagem da API: executa o bundle de migrations do EF Core, depois inicia a API com o argumento `carregar-demonstracao`, que roda a carga e encerra sem subir o servidor HTTP. A API só sobe depois que esse processo termina com sucesso. Com várias réplicas da API, aplicar migrations no startup faria cada réplica disputar o mesmo esquema, e uma migration lenta atrasaria a liveness e o readiness.

## Considered Options

- **Migrations no startup de cada réplica**: simples, mas exige lock entre réplicas e mistura o ciclo de vida do esquema com o da aplicação.
- **Init container em cada pod**: ainda roda uma vez por réplica e repete a disputa.
- **Job de uso único com o bundle do EF Core**: roda uma única vez por implantação, não exige o SDK na imagem final e permite reexecução porque as migrations e a carga são idempotentes.

## Consequences

- A imagem da API passa a levar o bundle de migrations (`efbundle`), gerado com a ferramenta `dotnet-ef` declarada no repositório.
- Rodar a API contra um banco sem migrations falha nas requisições até o Job terminar; testes de integração aplicam as migrations antes de entregar o cliente HTTP.
- A configuração `Demonstracao:Carregar` deixa de existir.
- Uma nova migration só chega ao banco quando o Job roda de novo na implantação.
