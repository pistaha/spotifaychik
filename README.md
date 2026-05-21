# spotifaychik

`spotifaychik` — учебный музыкальный сервис на ASP.NET Core с PostgreSQL. Репозиторий подготовлен под лабораторные 4 и 5: Nginx, TLS, балансировка нагрузки, CI/CD и стек наблюдаемости.

## Что реализовано

- multi-stage `Dockerfile` для API;
- один backend-сервис `app`, который по умолчанию запускается в трёх репликах;
- `nginx` как единственная внешняя точка входа;
- round-robin балансировка через `upstream`;
- TLS с self-signed сертификатом;
- раздача статических файлов через `/static/`;
- PostgreSQL с локальным volume;
- уникальный `X-Instance-Id` в ответах backend;
- `/metrics` для Prometheus;
- `Prometheus`, `Grafana`, `Loki`, `Promtail`, `nginx-prometheus-exporter`;
- GitHub Actions workflow для build, test, docker push и self-hosted deploy;
- запуск одной командой через `start.sh`.

## Ключевые файлы

- [docker-compose.yml](/Users/yarik/Rider/spotifaychik/docker-compose.yml)
- [MusicService.API/Dockerfile](/Users/yarik/Rider/spotifaychik/MusicService.API/Dockerfile)
- [nginx/default.conf](/Users/yarik/Rider/spotifaychik/nginx/default.conf)
- [MusicService.API/Program.cs](/Users/yarik/Rider/spotifaychik/MusicService.API/Program.cs)
- [start.sh](/Users/yarik/Rider/spotifaychik/start.sh)
- [check-lb.sh](/Users/yarik/Rider/spotifaychik/check-lb.sh)
- [docs/curl_examples.sh](/Users/yarik/Rider/spotifaychik/docs/curl_examples.sh)
- [docs/README.md](/Users/yarik/Rider/spotifaychik/docs/README.md)

## Структура

```text
spotifaychik/
├── MusicService.API/
│   └── Dockerfile
├── db/
│   └── init.sql
├── nginx/
│   ├── default.conf
│   ├── certs/
│   │   ├── server.crt
│   │   └── server.key
│   └── static/
│       └── style.css
├── docs/
│   ├── README.md
│   ├── curl_examples.sh
│   └── screenshots/
├── grafana/
├── loki/
├── prometheus/
├── promtail/
├── .env
├── docker-compose.yml
└── start.sh
```

## Генерация сертификата

Если файлов `nginx/certs/server.crt` и `nginx/certs/server.key` нет:

```bash
mkdir -p nginx/certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/certs/server.key \
  -out nginx/certs/server.crt \
  -subj "/CN=localhost"
```

## Запуск

```bash
chmod +x start.sh docs/curl_examples.sh
./start.sh
```

После запуска:

- `https://localhost/`
- `https://localhost/swagger`
- `https://localhost/static/style.css`
- `https://localhost/metrics`
- `http://localhost:3000`
- `http://localhost:9090`

## Что происходит в Compose

- `db` поднимает PostgreSQL;
- backend описан одним сервисом `app` через общий шаблон `x-app-common`;
- у backend-инстансов нет опубликованных внешних портов;
- `nginx` публикует только `80` и `443`;
- Nginx проксирует трафик на DNS-имя `app`, а Docker DNS отдаёт доступные реплики;
- каждый backend отвечает своим `X-Instance-Id`.
- Prometheus собирает метрики с API и Nginx exporter.
- Grafana поднимает готовый dashboard из provisioning.
- Promtail собирает Docker-логи и отправляет их в Loki.
- Отдельный сервис `docker-events` пишет runtime-события Docker (`oom`, `die`, `restart`) в общий поток логов.
- Логи контейнеров в Loki размечаются labels `compose_service`, `container`, `image`, `stream`.

## Масштабирование backend

Структура сделана так, чтобы конфиг не менялся при добавлении нового backend-инстанса.
Для этого в Compose используется один сервис `app`, а количество инстансов задаётся через `--scale`.

Запуск по умолчанию:

```bash
docker compose --env-file .env up -d --build --scale app=3
```

Добавление четвёртой реплики:

```bash
docker compose --env-file .env up -d --scale app=4
```

После этого не нужно править `docker-compose.yml`, `nginx/default.conf` и `prometheus/prometheus.yml`.

## Проверка балансировки

```bash
for i in 1 2 3 4 5 6; do
  curl -sk -D - https://localhost/ -o /tmp/resp.json | grep -i x-instance-id
  cat /tmp/resp.json
  echo
done
```

Или готовым скриптом:

```bash
./docs/curl_examples.sh
```

Или короткой командой, которая сразу выводит имена реплик:

```bash
./check-lb.sh
```

Ожидаемо в заголовках и JSON будут появляться разные значения `X-Instance-Id`, соответствующие именам контейнеров реплик `app`.

## Проверка статики

```bash
curl -sk https://localhost/static/style.css
```

Этот запрос обслуживает Nginx напрямую, без проксирования в backend.

## Проверка backend

Проверить текущее состояние сервисов:

```bash
docker compose --env-file .env ps
```

Проверить root endpoint через Nginx:

```bash
curl -sk https://localhost/
```

Проверить health:

```bash
curl -sk https://localhost/health
```

## Что показать на защите

1. [docker-compose.yml](/Users/yarik/Rider/spotifaychik/docker-compose.yml): один сервис `app`, масштабирование через `--scale`, `nginx`, `db`, отсутствие внешних портов у backend.
2. [MusicService.API/Dockerfile](/Users/yarik/Rider/spotifaychik/MusicService.API/Dockerfile): multi-stage сборка.
3. [nginx/default.conf](/Users/yarik/Rider/spotifaychik/nginx/default.conf): `upstream`, `proxy_pass`, `ssl_certificate`, `/static/`.
4. [MusicService.API/Program.cs](/Users/yarik/Rider/spotifaychik/MusicService.API/Program.cs): `X-Instance-Id`.
5. Команды:

```bash
./start.sh
docker compose --env-file .env ps
./docs/curl_examples.sh
curl -sk https://localhost/static/style.css
```

Подробная инструкция по сдаче и lab 5 находится в [docs/README.md](/Users/yarik/Rider/spotifaychik/docs/README.md).
