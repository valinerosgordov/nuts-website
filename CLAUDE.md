# Nuts (Ореховый Сад) — Project Instructions

> **Полный контекст проекта:** `C:\ObsidianVault\_Claude\projects.md` (раздел Nuts)
> **Общий контекст Vitaly:** `C:\ObsidianVault\_Claude\README.md`

---

## Quick reference

- **Type**: e-commerce, премиум магазин орехов и сухофруктов
- **URL**: http://217.199.252.33 (нужен domain + HTTPS)
- **Stack**: ASP.NET Core 10 Minimal API + Clean Arch + EF Core SQLite + Docker + Nginx
- **Integration**: МойСклад API (`MOYSKLAD_TOKEN`)
- **Auth**: JWT + PBKDF2 для админа
- **CI/CD**: GitHub Actions — автодеплой на push в master
- **Client**: Stanislav → его друг-владелец
- **Status**: 🟢 Prod на голом IP

## Перед работой

1. `C:\ObsidianVault\_Claude\README.md`
2. `C:\ObsidianVault\_Claude\active-work.md` — актуальные TODO
3. Раздел Nuts в `C:\ObsidianVault\_Claude\projects.md`

## TODO

- [ ] Подключить domain
- [ ] HTTPS через Certbot
- [ ] **CVE-2026-40372** — обновить `Microsoft.AspNetCore.DataProtection` до 10.0.7 если используется
- [ ] Если потребуется миграция PBKDF2 → bcrypt/Argon2id — делать через multi-algorithm pattern (см. `C:\ObsidianVault\C#\AspNetCore\security-practices.md#password-hashing-migration`)

## Правила

- `MOYSKLAD_TOKEN` — только через env var, **не** в `appsettings.json` в git
- При обновлении статуса → `C:\ObsidianVault\_Claude\projects.md`
