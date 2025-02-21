# Vault Secret Rotation Demo

Демонстрационный проект для показа механизма ротации секретов с использованием HashiCorp Vault.

## Описание

Проект состоит из трех основных компонентов:

1. **Vault Server** - HashiCorp Vault в dev режиме
2. **Platform.Storage** - Сервис, управляющий секретами (порт 5000)
3. **Domain.Service** - Сервис-потребитель секретов (порт 5001)

## Требования

- Docker и Docker Compose
- .NET 6.0 SDK
- Bash (для Linux/MacOS) или PowerShell (для Windows)

## Структура проекта

```
VaultDemo/
├── Platform.Storage/        # Сервис управления секретами
├── Domain.Service/         # Сервис-потребитель
├── docker-compose.yml      # Конфигурация Vault
└── init.sh                 # Скрипт инициализации
```

## Запуск

1. Запустите Vault:
```bash
docker-compose up -d
```

2. Запустите Platform.Storage:
```bash
cd Platform.Storage
dotnet run
```

3. Запустите Domain.Service:
```bash
cd Domain.Service
dotnet run
```

## Как это работает

1. **Platform.Storage** каждые 30 секунд генерирует новый секрет и сохраняет его в Vault
2. **Domain.Service** периодически опрашивает Platform.Storage, передавая текущий секрет
3. Platform.Storage проверяет валидность секрета, сравнивая его с двумя последними версиями

## Конфигурация

Основные настройки находятся в файлах appsettings.json:

### Platform.Storage
```json
{
  "Vault": {
    "Url": "http://localhost:8200",
    "Token": "root"
  }
}
```

### Domain.Service
```json
{
  "PollingService": {
    "TargetUrl": "http://localhost:5000/content"
  },
  "Vault": {
    "Url": "http://localhost:8200",
    "Token": "root"
  }
}
```

## Важные замечания

- Проект использует Vault в dev режиме - не используйте такую конфигурацию в production
- Root token захардкожен для простоты демонстрации
- В production необходимо использовать безопасные методы аутентификации и правильную настройку ACL

## Очистка

Для полной очистки данных Vault:

```bash
docker-compose down -v
```

## Лицензия

MIT