// Teste de carga do requisito não funcional:
// "Em picos, o serviço de consolidado recebe 50 requisições/s, com no máximo 5% de perda."
//
// Uso (com o docker-compose no ar):
//   k6 run tests/load/consolidated-balance-50rps.js
//   k6 run -e BASE_URL=http://localhost:5002 -e RATE=100 tests/load/consolidated-balance-50rps.js
import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5002';
const RATE = Number(__ENV.RATE || 50);

export const options = {
  scenarios: {
    pico_consolidado: {
      executor: 'constant-arrival-rate', // taxa fixa de chegada, independente da latência
      rate: RATE,
      timeUnit: '1s',
      duration: '1m',
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],     // requisito: perda máxima de 5%
    http_req_duration: ['p(95)<300'],   // meta de latência
  },
};

function randomRecentDate() {
  const d = new Date();
  d.setDate(d.getDate() - Math.floor(Math.random() * 30));
  return d.toISOString().slice(0, 10);
}

export default function () {
  const res = Math.random() < 0.8
    ? http.get(`${BASE_URL}/api/v1/daily-balances/${randomRecentDate()}`)
    : http.get(`${BASE_URL}/api/v1/daily-balances?from=${randomRecentDate()}&to=${new Date().toISOString().slice(0, 10)}`);

  check(res, { 'status 200': (r) => r.status === 200 });
}
