# Мастер‑проект .NET: музыка, сервисы и учебные лабораторные

Этот репозиторий — личный monorepo с несколькими независимыми решениями на .NET. Здесь есть:
- полнофункциональный музыкальный сервис с БД и авторизацией,
- облегченный API с файловым хранилищем,
- учебные лабораторные и эксперименты (собственный HTTP‑сервер, алгоритмы и структуры данных, консольные проекты).

## Что внутри

| Проект/решение | Кратко | Запуск |
|---|---|---|
| `MusicServer.sln` | REST API для музыки: треки, плейлисты, пользователи, фавориты; файловое (JSON) хранилище; Swagger; стриминг аудио | `dotnet run --project MusicServer.API` |
| `MusicServerLab2/MusicServerLab2.sln` | Полноценный music‑service: Postgres + EF Core + FluentMigrator, JWT‑аутентификация, загрузка файлов, поиск, админ‑аудит | `dotnet run --project MusicServerLab2/MusicService.API` |
| `MusicLab1.sln` | Мини‑HTTP сервер без ASP.NET: маршрутизация, контроллеры, HTML‑шаблоны, управление списком песен | `dotnet run --project MusicLab1` |
| `MusicLab2.sln` | Заготовка под 2‑ю лабу: модели Artist/Track/Playlist/User, файловые репозитории, minimal API шаблон | `dotnet run --project MusicLab2.API` |
| `ASD lab1/ASD lab1.sln` | Лаба по АиСД: собственный АТД‑список и удаление дубликатов | `dotnet run --project "ASD lab1/моя первая прекрасная лаболаторная по Алгоритмам и струтурам данных /ASD lab.csproj"` |
| `Dvizhki/Dvizhki.sln` | Консольный стартовый проект | `dotnet run --project "Dvizhki/Dvizhki yep/Dvizhki yep.csproj"` |

## Технологии

- .NET 10 (preview), C#
- ASP.NET Core (Web API, Swagger/OpenAPI)
- EF Core + PostgreSQL (для `MusicServerLab2`)
- FluentValidation, AutoMapper, FluentMigrator
- JWT, refresh tokens, security audit (в `MusicServerLab2`)
- Файловые репозитории и JSON‑хранилище (в `MusicServer`)
- xUnit/Moq/FluentAssertions для тестов

## Быстрый старт

1) Установите .NET SDK 10 (preview). В `global.json` включен `allowPrerelease`.
2) Клонируйте репозиторий и выберите нужное решение.
3) Запустите проект командой из таблицы выше.

Пример для `MusicServer`:
```bash
dotnet restore
dotnet run --project MusicServer.API
```

## Конфигурация

`MusicServer` хранит JSON и аудио‑файлы локально:
- конфиг: `MusicServer.API/appsettings.json`
- базовый путь: `Data/Json`, `Data/AudioFiles`

`MusicServerLab2` использует PostgreSQL и миграции:
- конфиг: `MusicServerLab2/MusicService.API/appsettings.json`
- строка подключения: `Database:ConnectionString`
- при старте автоматически накатываются миграции FluentMigrator

## Тесты

```bash
dotnet test MusicServer.Tests
dotnet test MusicLab1.Tests
dotnet test MusicServerLab2/tests/tests.csproj
```

## Важное про пути

В репозитории есть папки с ведущими/замыкающими пробелами в имени (например ` MusicLab2.Core` или каталог АиСД‑лабы).
Если запускаете проекты через CLI — используйте кавычки.

## Структура репозитория (кратко)

- `MusicServer.*` — API с файловым хранилищем, сервисный слой и тесты
- `MusicServerLab2/*` — многослойный сервис с БД, auth, файлами и миграциями
- `MusicLab1` — самописный HTTP‑сервер и веб‑вьюхи
- `MusicLab2.*` — модели, репозитории, минимальный API‑скелет
- `ASD lab1` — лаба по структурам данных
- `Dvizhki` — консольный стартовый проект

## План развития (идеи)

- привести структуры папок к единому стилю
- добавить общую документацию по API (Postman/Swagger ссылки)
- автоматизировать запуск через docker‑compose для `MusicServerLab2`
