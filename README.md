# Ореховый Сад

Премиум интернет-магазин орехов и сухофруктов.

**Прод:** http://217.199.252.33

## Стек
- ASP.NET Core 10 (Minimal API)
- Clean Architecture (Domain → Application → Infrastructure → Api)
- EF Core 10 + SQLite
- Docker + Nginx + GitHub Actions
- Интеграция с МойСклад

## Быстрый старт

### Требования
- .NET 10 SDK
- Docker (опционально)

### Локальный запуск
```bash
cd src/Nuts.Api
# Установить переменные окружения (см. ниже)
dotnet run --urls "http://localhost:5100"
```

Открыть http://localhost:5100

### Сгенерировать хэш пароля админа
```bash
dotnet run --project src/Nuts.Api -- hash-password "YourPassword"
```
Вывод (`salt:hash`) положить в `Admin:PasswordHash` или env `ADMIN_PASSWORD_HASH`.

## Структура проекта
- `src/Nuts.Domain` — сущности, бизнес-правила
- `src/Nuts.Application` — интерфейсы репозиториев
- `src/Nuts.Infrastructure` — EF Core, МойСклад, репозитории
- `src/Nuts.Api` — Minimal API + фронтенд (wwwroot)
- `tests/Nuts.Tests` — unit-тесты

## Обязательные env переменные
- `JWT_SECRET_KEY` — ключ подписи JWT (минимум 32 байта)
- `MOYSKLAD_TOKEN` — токен МойСклад API
- `CORS_ORIGINS` — разрешённые origins через запятую
- `ADMIN_PASSWORD_HASH` — PBKDF2 хэш в формате `salt:hash` (base64)

## Деплой на прод
GitHub Actions автодеплоит при push в `master` (см. `.github/workflows/deploy.yml`).

Ручной деплой:
```bash
ssh root@217.199.252.33
cd /opt/nuts && git pull origin master && docker compose up -d --build
```

## Админка
http://217.199.252.33/admin/

## Тесты
```bash
dotnet test tests/Nuts.Tests
```

## Бэкапы

На проде настроен ежедневный автобэкап БД (`nuts.db`) и директории `uploads/`.

- **Расписание:** ежедневно в 03:00 по серверному времени (cron)
- **Хранилище:** `/opt/nuts/backups/` на сервере
- **Ретенция:** последние 14 копий (старше — удаляются автоматически)
- **Скрипты:** `/opt/nuts/backup.sh`, `/opt/nuts/restore.sh`
- **Логи:** `/var/log/nuts-backup.log`

### Ручной бэкап
```bash
ssh root@217.199.252.33
/opt/nuts/backup.sh
```

### Восстановление
```bash
ssh root@217.199.252.33
ls /opt/nuts/backups/                       # посмотреть доступные timestamps
/opt/nuts/restore.sh YYYYMMDD_HHMMSS        # например 20260424_150641
```
Скрипт восстановит `nuts.db` в контейнер `nuts-api`, перезапустит контейнер и распакует `uploads.tar.gz` в volume.

## Мониторинг

На сервере работает healthcheck каждые 5 минут (cron, `/opt/nuts/healthcheck.sh`):
если `curl http://localhost/` не отвечает за 10 секунд — выполняется `docker compose restart`. Логи: `/var/log/nuts-health.log`.
