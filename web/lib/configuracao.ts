import "server-only";

function variavel(nome: string, padrao?: string): string {
  const valor = process.env[nome] ?? padrao;
  if (!valor) {
    throw new Error(`Variavel de ambiente ${nome} nao configurada.`);
  }
  return valor;
}

function semBarraFinal(url: string): string {
  return url.replace(/\/+$/, "");
}

export const configuracao = {
  get apiUrl() {
    return semBarraFinal(variavel("API_URL", "http://localhost:8081"));
  },
  get keycloakUrlPublica() {
    return semBarraFinal(variavel("KEYCLOAK_URL_PUBLICA", "http://localhost:8080/realms/oficina"));
  },
  get keycloakUrlInterna() {
    return semBarraFinal(variavel("KEYCLOAK_URL_INTERNA", this.keycloakUrlPublica));
  },
  get keycloakClientId() {
    return variavel("KEYCLOAK_CLIENT_ID", "oficina-web");
  },
  get keycloakClientSecret() {
    return variavel("KEYCLOAK_CLIENT_SECRET");
  },
};
