# App iOS chama a API direto, com o token no Keychain

O app iOS do Mecânico (`ios/`, SwiftUI) faz o login no Keycloak com um client público próprio (`oficina-ios`), por authorization code + PKCE numa `ASWebAuthenticationSession`, e chama a API direto com o JWT. O access token e o refresh token ficam no Keychain do aparelho. A ADR 0007 tirou o token do navegador porque ali qualquer script da página o alcança e porque chamar a API do navegador exigiria CORS. Num app nativo, nenhum desses motivos vale: o Keychain isola o token por app, não há script de terceiros no processo, e a requisição de um app não passa por CORS. Um BFF para o app seria um servidor a mais só para guardar um segredo que o app já guarda bem.

## Considered Options

- **Passar pelo BFF da web**: o BFF do Next.js trabalha com sessão em cookie e Server Actions, não expõe uma API para outro cliente. Usá-lo exigiria criar e manter endpoints só para o app.
- **Login com usuário e senha no próprio app (password grant)**: o app passaria a manusear a senha do funcionário e dependeria de um grant que o OAuth 2.1 abandona. Foi descartado pelo mesmo motivo na ADR 0007.
- **Client confidencial**: um secret embutido no app pode ser extraído do binário, então não protege nada. O PKCE cumpre esse papel num client público.

## Consequences

- O realm ganha um terceiro client, com redirect por scheme próprio (`oficina://autenticacao`) e o mesmo audience mapper `oficina-api` dos outros.
- A API não muda por causa do login: ela continua validando issuer, audience e papel.
- O issuer dos tokens é `http://localhost:8080`, então o app roda só no Simulador do iOS. Um iPhone físico exigiria outro hostname no Keycloak e liberar HTTP sem TLS.
