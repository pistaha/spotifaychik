# Лабораторные 4 и 5

Этот проект покрывает обе лабораторные работы:

- лабораторная 4: `Nginx`, `TLS`, статика и round-robin балансировка между тремя backend-инстансами;
- лабораторная 5: `CI/CD`, self-hosted deploy, `Prometheus`, `Grafana`, `Loki`, `Promtail` и acceptance gate.

## Что реализовано

### Лабораторная 4

- один backend-сервис `app`, который по умолчанию запускается в трёх репликах;
- `nginx` как единственная внешняя точка входа;
- самоподписанный TLS-сертификат;
- `upstream` с round-robin балансировкой;
- уникальный `X-Instance-Id` и `instanceId` в ответе сервера;
- раздача файлов из `nginx/static/` через `/static/`;
- прямой доступ к backend-контейнерам извне закрыт, опубликованы только порты `80` и `443`.

### Лабораторная 5

- endpoint `/metrics` в ASP.NET через `prometheus-net.AspNetCore`;
- весь backend и CI/CD собраны на стабильном `.NET 8`;
- `Prometheus` для сбора метрик приложения и Nginx;
- `nginx-prometheus-exporter` + `stub_status`;
- `Loki` и `Promtail` для централизованного сбора логов контейнеров;
- `Grafana` с автоподключением datasources и готовым dashboard;
- GitHub Actions pipeline:
  - `build`
  - `docker-build-push`
  - `notify`
  - `deploy`
  - `verify` как acceptance gate внутри job `deploy`.

## Структура

```text
.
├── .github/workflows/ci.yml
├── MusicService.API/
│   ├── Dockerfile
│   └── Program.cs
├── nginx/
│   ├── default.conf
│   ├── prometheus.conf
│   ├── certs/
│   └── static/
├── prometheus/prometheus.yml
├── loki/loki-config.yml
├── promtail/config.yml
├── grafana/provisioning/
│   ├── datasources/datasources.yml
│   └── dashboards/
│       ├── dashboards.yml
│       └── main.json
├── docker-compose.yml
├── docs/
│   ├── README.md
│   ├── curl_examples.sh
│   └── screenshots/
└── start.sh
```

## Подготовка

1. Создать `.env` на основе `.env.example`.
2. При необходимости сгенерировать сертификат:

```bash
mkdir -p nginx/certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/certs/server.key \
  -out nginx/certs/server.crt \
  -subj "/CN=localhost"
```

3. Убедиться, что Docker запущен.

## Запуск

```bash
chmod +x start.sh docs/curl_examples.sh
./start.sh
```

После старта должны быть доступны:

- `https://localhost/`
- `https://localhost/health`
- `https://localhost/metrics`
- `https://localhost/static/style.css`

## Остановка

```bash
docker compose --env-file .env down
```

## Быстрые ссылки

```bash
open https://localhost/
open http://localhost:9090
open http://localhost:3000
```

## Масштабирование backend

`docker-compose.yml` содержит один сервис `app` на базе общего шаблона `x-app-common`.
`nginx` и `prometheus` смотрят на имя сервиса `app`, а Docker DNS сам отдаёт доступные реплики.

Запуск по умолчанию:

```bash
docker compose --env-file .env up -d --build --scale app=3
```

Добавление четвёртого backend-инстанса:

```bash
docker compose --env-file .env up -d --scale app=4
```

После этого не нужно менять `docker-compose.yml`, `nginx/default.conf` и `prometheus/prometheus.yml`.

## Конфигурация Nginx

Файл: [nginx/default.conf](/Users/yarik/Rider/spotifaychik/nginx/default.conf)

- `upstream music_service_backend` использует DNS-имя `app`, а Docker DNS отдаёт доступные backend-реплики;
- `location /` проксирует запросы в upstream;
- `location /static/` обслуживается локально самим Nginx;
- `listen 443 ssl` использует `server.crt` и `server.key`;
- `listen 80` выполняет редирект на HTTPS.

Файл: [nginx/prometheus.conf](/Users/yarik/Rider/spotifaychik/nginx/prometheus.conf)

- поднимает внутренний endpoint `/nginx_status` для `nginx-prometheus-exporter`.

## Проверка лабораторной 4

### Балансировка нагрузки

```bash
for i in 1 2 3 4 5 6; do
  curl -sk -D - https://localhost/ -o /tmp/resp.json | grep -i x-instance-id
  cat /tmp/resp.json
  echo
done
```

Ожидается чередование разных `X-Instance-Id`, соответствующих именам контейнеров реплик `app`.

Для удобной проверки с выводом сразу `app-1`, `app-2`, `app-3`:

```bash
./check-lb.sh
```

### Статические файлы

```bash
curl -sk https://localhost/static/style.css
```

### Проверка закрытия прямого доступа

Из хоста backend-контейнеры недоступны напрямую, потому что их порты не опубликованы. Проверка с хоста:

```bash
curl --max-time 3 http://localhost:8080
```

Ожидается `Connection refused` или аналогичная ошибка.

DNS-имя `app` разрешается только внутри docker-сети и используется для балансировки и сбора метрик.

Дополнительно проверьте список опубликованных портов:

```bash
docker compose ps
```

В выводе опубликованные порты должны быть только у `nginx`, `grafana` и `prometheus`.
В выводе опубликованные порты для сдачи лабораторной 4 должны быть только у `nginx`.

## Проверка лабораторной 5

### Метрики приложения

```bash
curl -k https://localhost/health
curl -k https://localhost/metrics
curl -s http://localhost:9090/api/v1/targets
curl -s "http://localhost:9090/api/v1/query" --data-urlencode 'query=up'
```

Grafana должна открываться с хоста по адресу:

```bash
open http://localhost:3000
```

### Логи контейнеров

`Promtail` собирает stdout/stderr всех docker-контейнеров через `docker_sd_configs` и отправляет их в `Loki`.
Дополнительно сервис `docker-events` пишет в stdout runtime-события Docker для текущего compose-проекта:

- `oom`
- `die`
- `restart`

Это позволяет видеть в `Loki/Grafana`, что контейнер был убит runtime'ом, даже если само приложение не успело записать финальный лог.

Для каждого лога доступны labels:

- `job="docker"`
- `compose_project`
- `compose_service`
- `container`
- `container_id`
- `image`
- `stream`

Быстрые запросы в `Grafana -> Explore`:

```logql
{job="docker"}
```

```logql
{compose_service="app"}
```

```logql
{compose_service="nginx"}
```

```logql
{compose_service="app",stream="stderr"}
```

```logql
{compose_service="docker-events"}
```

### Нагрузочный тест

```bash
for i in $(seq 1 50); do
  curl -sk https://localhost/ > /dev/null
done
```

После этого в Grafana на дашборде должны измениться:

- `RPS`
- `Latency p99`
- `HTTP 5xx Rate`
- `Nginx Logs`

### Проверка разных backend-инстансов

```bash
for i in $(seq 1 20); do
  curl -sk https://localhost/
  echo
done
```

В ответах должен меняться `instanceId`, например `app-1`, `app-2`, `app-3`.

### Проверка Prometheus targets

```bash
curl -s http://localhost:9090/api/v1/targets
```

Ожидается `UP` для:

- `job="app"` у нескольких реплик;
- `job="nginx"` у `nginx-exporter`.

### Проверка Grafana dashboard

Открыть Grafana:

```bash
open http://localhost:3000
```

Проверить dashboard `Music Service Observability`. На нём должны быть панели:

- `RPS`
- `Latency p99`
- `HTTP 5xx Rate`
- `Nginx Logs`

### Проверка Loki / Grafana Explore

В `Grafana -> Explore` можно использовать запросы:

```logql
{compose_service="nginx"}
```

```logql
{compose_service="app"}
```

### Проверка acceptance gate

Workflow в [ci.yml](/Users/yarik/Rider/spotifaychik/.github/workflows/ci.yml) после деплоя:

1. ждёт запуск контейнеров;
2. проверяет `curl -fk https://localhost/health`;
3. запрашивает у `Prometheus` текущий `5xx rate`;
4. завершает job ошибкой, если `5xx rate > 0.05`.

То есть gate реально блокирует deploy в двух случаях:

- `/health` возвращает не `200`, потому что используется `curl -fk https://localhost/health`;
- `5xx rate > 0.05`, потому что число проверяется через `python3` и при превышении workflow завершается с ошибкой.

## CI/CD

Pipeline запускается на push в `main` и состоит из job:

- `build`
- `docker-build-push`
- `notify`
- `deploy`

`docker-build-push` публикует образ:

- `pistahas/spotifaychik:latest`
- `pistahas/spotifaychik:<short_sha>`

`deploy` выполняется на `self-hosted runner`, использует secrets:

- `DOCKER_USERNAME`
- `DOCKER_PASSWORD`
- `DEPLOY_PATH`

Во время деплоя runner:

1. заходит в `DEPLOY_PATH`;
2. проверяет наличие `docker-compose.yml`;
3. экспортирует `APP_IMAGE_TAG=latest`;
4. экспортирует `DOCKER_IMAGE_REPOSITORY="${DOCKER_USERNAME}/spotifaychik"`;
5. выполняет `docker compose pull`;
6. выполняет `docker compose up -d --no-build --scale app=3`.

## GitHub Secrets

Для работы CI/CD нужно задать:

- `DOCKER_USERNAME`
- `DOCKER_PASSWORD`
- `DEPLOY_PATH`

`DEPLOY_PATH` должен указывать на каталог проекта на машине со `self-hosted runner`, где лежит `docker-compose.yml`.

## Скриншоты для сдачи

В каталог `docs/screenshots/` нужно сохранить:

- `successful-pipeline.png` — успешный прогон GitHub Actions со всеми зелёными job.
- `deploy-verify-green.png` — зелёные `deploy` и `Verify deployment`.
- `blocked-deploy.png` — проваленный deploy после намеренно сломанного `/health`.
- `prometheus-targets.png` — targets со статусом `UP`.
- `grafana-dashboard.png` — Grafana dashboard с живыми графиками.
- `grafana-logs.png` — Grafana Explore с логами из Loki.

## Self-hosted runner

Runner регистрируется в GitHub:

1. `Settings`
2. `Actions`
3. `Runners`
4. `New self-hosted runner`

После установки runner должен иметь доступ к Docker без `sudo`.

## Что показать на защите

1. [docker-compose.yml](/Users/yarik/Rider/spotifaychik/docker-compose.yml)
2. [MusicService.API/Program.cs](/Users/yarik/Rider/spotifaychik/MusicService.API/Program.cs)
3. [nginx/default.conf](/Users/yarik/Rider/spotifaychik/nginx/default.conf)
4. [nginx/prometheus.conf](/Users/yarik/Rider/spotifaychik/nginx/prometheus.conf)
5. [prometheus/prometheus.yml](/Users/yarik/Rider/spotifaychik/prometheus/prometheus.yml)
6. [grafana/provisioning/dashboards/main.json](/Users/yarik/Rider/spotifaychik/grafana/provisioning/dashboards/main.json)
7. [docs/curl_examples.sh](/Users/yarik/Rider/spotifaychik/docs/curl_examples.sh)
