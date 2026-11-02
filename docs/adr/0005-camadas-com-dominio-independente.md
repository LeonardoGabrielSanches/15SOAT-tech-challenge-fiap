# Monólito em camadas com o domínio no centro

O monólito tem quatro camadas: **Domain**, **Application**, **Infrastructure** e **Api**. As dependências sempre apontam para o domínio. O `Domain` não referencia nenhum outro projeto nem pacote de infraestrutura: nada de EF Core, ASP.NET ou atributos de mapeamento. Ele é rico: entidades e objetos de valor garantem as próprias invariantes, como transições de status, CPF/CNPJ, placa e quantidades, e não dependem de validação no banco. A `Application` contém os casos de uso, que orquestram agregados e definem a transação. Ela também verifica as regras que exigem consultar outros agregados, como uma OS em andamento por veículo, placa única e CPF/CNPJ único. As interfaces de repositório ficam no `Domain`, e as de serviços técnicos (notificador, unidade de trabalho) ficam na `Application`. A `Infrastructure` implementa as interfaces (repositórios com EF Core/Npgsql e o notificador simulado). A `Api` expõe REST, autentica via Keycloak e documenta com OpenAPI.

## Considered Options

- **Casos de uso dentro do Domain**: menos projetos, mas mistura orquestração e transação com regras de negócio.
