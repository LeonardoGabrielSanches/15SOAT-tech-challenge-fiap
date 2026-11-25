import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = (__ENV.BASE_URL || "http://localhost:5000").replace(/\/$/, "");
const USUARIO = __ENV.USUARIO || "gerente";
const SENHA = __ENV.SENHA || "gerente123";
const CLIENTE_OIDC = __ENV.CLIENTE_OIDC || "oficina-swagger";
const URL_TOKEN = __ENV.URL_TOKEN || `${BASE_URL}/auth/realms/oficina/protocol/openid-connect/token`;

export const options = {
  scenarios: {
    pico: {
      executor: "ramping-vus",
      startVUs: 10,
      stages: [
        { duration: "1m", target: 60 },
        { duration: `${__ENV.DURACAO_MINUTOS || 4}m`, target: 120 },
        { duration: "30s", target: 0 },
      ],
      gracefulRampDown: "10s",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.05"],
  },
};

export function setup() {
  const resposta = http.post(URL_TOKEN, {
    grant_type: "password",
    client_id: CLIENTE_OIDC,
    username: USUARIO,
    password: SENHA,
  });
  if (resposta.status !== 200) {
    throw new Error(`Falha ao obter token (${resposta.status}): ${resposta.body}`);
  }
  return { token: resposta.json("access_token") };
}

const ROTAS = [
  "/api/administrativo/clientes",
  "/api/administrativo/ordens-de-servico",
  "/api/administrativo/ordens-de-servico/fila-de-execucao",
  "/api/administrativo/ordens-de-servico/tempo-medio-de-execucao",
  "/api/administrativo/servicos",
  "/api/pecas-insumos",
];

export default function (dados) {
  const cabecalhos = { headers: { Authorization: `Bearer ${dados.token}` } };
  for (const rota of ROTAS) {
    const resposta = http.get(`${BASE_URL}${rota}`, cabecalhos);
    check(resposta, { [`${rota} 200`]: (r) => r.status === 200 });
  }
  const saude = http.get(`${BASE_URL}/saude`);
  check(saude, { "saude 200": (r) => r.status === 200 });
  sleep(0.2);
}
