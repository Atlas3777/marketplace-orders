
# Marketplace Orders Service
Микросервис управления заказами для e-commerce платформы. Обеспечивает создание заказов, расчет стоимости, отслеживание статусов, кэширование и синхронное взаимодействие с каталогом товаров.

## Стек
* **Backend:** .NET 8 (C# 12) / ASP.NET Core Web API
* **Inter-service Communication:** gRPC Client (для связи с Marketplace.Products)
* **Primary Database:** PostgreSQL + Dapper (с ручным управлением транзакциями)
* **Caching:** Redis (паттерн Cache-Aside с инвалидацией постраничных выборок через версии пользователей)
* **Validation:** FluentValidation
* **Migrations:** FluentMigrator

## Структура проекта
* `Domain` — ядро системы. Содержит основные сущности (`Order`, `OrderItem`) и логику статусов заказа (`OrderStatus`).
* `Application` — бизнес-логика приложения (`OrderService`). Включает в себя контракты репозиториев, gRPC-клиентов, DTO и валидаторы входных данных.
* `Infrastructure` — транспортный и инфраструктурный уровень. Реализует работу с PostgreSQL через Dapper (`OrderRepository`), слой кэширования (`CachedOrderRepository`) и gRPC-клиент (`ProductGrpcClient`) для получения информации о ценах на товары.
* `Api` — точка входа и внешние интерфейсы (REST Controllers). Содержит мапперы, конфигурацию DI в `Program.cs` и `ExceptionHandlingMiddleware` для корректной обработки исключений.
* `Migrations` — миграции схемы базы данных на базе FluentMigrator.

---

### 1. Подъем инфраструктуры
Для работы сервиса необходимы PostgreSQL и Redis. Настройки контейнеров описаны в файле `compose.yaml`.

Убедитесь, что у вас установлен **Docker**, и выполните команду:
``` bash
docker compose up -d
```

Команда запустит:

* **PostgreSQL** на порту `5436` (база данных `marketplace-orders-db`)
* **Redis** на порту `6380`

### 2. Конфигурация приложения

Перед запуском проверьте строки подключения в конфигурационном файле (например, `appsettings.json` в проекте Api):

```json
{
  "ConnectionStrings": {
    "OrdersDb": "Host=localhost;Port=5436;Database=marketplace-orders-db;Username=postgres;Password=postgres",
    "Redis": "localhost:6380"
  },
  "GrpcServices": {
    "Products": "http://localhost:5107"
  }
}

```

### 3. Запуск приложения и миграций

При запуске сервис автоматически проверяет состояние базы данных и накатывает недостающие миграции перед стартом веб-сервера.

Выполните команду для запуска:

```bash
dotnet run --project Marketplace.Orders.Api
```

### 4. Использование API

После запуска эндпоинты и интерактивная документация Swagger будут доступны по адресу: `http://localhost:<порт_приложения>/swagger`

**Основные эндпоинты:**

* `POST /api/v1/orders` — Создание нового заказа (автоматически запрашивает актуальные цены товаров из сервиса продуктов через gRPC).
* `GET /api/v1/orders/{id}` — Получение детальной информации о заказе по его Идентификатору (с поддержкой кэширования).
* `GET /api/v1/orders` — Постраничное получение списка заказов пользователя (`UserId`, `PageIndex`, `PageSize`).
* `HttpPatch /api/v1/orders/{id}/status` — Обновление статуса заказа (с автоматическим сбросом устаревшего кэша).