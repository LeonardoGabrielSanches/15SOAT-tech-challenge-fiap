# Keycloak como provedor de identidade das APIs administrativas

As APIs administrativas exigem JWT e três papéis distintos (Atendente, Mecânico, Gerente). Em vez de emitir e validar tokens na própria aplicação, usamos o Keycloak no `docker-compose.yml`: ele cadastra usuários e papéis e emite os JWTs, e a aplicação só valida o token e checa o papel. Isso tira do monólito o cadastro de usuários, senhas e assinatura de tokens. O Cliente não tem conta no Keycloak: ele consulta a OS pelo número da OS + CPF/CNPJ e aprova ou recusa o orçamento com um token de uso único.
