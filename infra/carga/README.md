# Teste de carga

Roteiro k6 que gera pico de CPU na API para demonstrar o HPA (2 a 5 replicas).

## Autenticacao

O roteiro obtem um token por password grant no client `oficina-swagger` com o usuario de demonstracao `gerente`. O realm ja habilita esse fluxo para o Swagger, entao nada no realm foi afrouxado. Os endpoints exercitados sao listagens do administrativo e do estoque, que passam pela validacao do JWT e pelo banco.

## Uso local

Com o compose no ar:

    k6 run -e BASE_URL=http://localhost:5000 -e URL_TOKEN=http://localhost:8080/realms/oficina/protocol/openid-connect/token infra/carga/carga.js

Contra o cluster:

    k6 run -e BASE_URL=http://<dns-do-load-balancer> infra/carga/carga.js

Variaveis: `BASE_URL`, `URL_TOKEN`, `USUARIO`, `SENHA`, `CLIENTE_OIDC`, `DURACAO_MINUTOS` (patamar de pico, padrao 4).

## Acompanhamento

    kubectl get hpa -n oficina -w
