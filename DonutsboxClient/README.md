# Donutsbox Client

Angular приложение с Server-Side Rendering (SSR) для Donutsbox.

## 🚀 Запуск в Docker

Frontend автоматически собирается и запускается через `docker-compose.yml` в `DonutsboxServer/`.

### Из корня проекта:

```bash
cd DonutsboxServer
sudo docker compose up -d --build frontend
```

### Проверка:

```bash
# Прямой доступ к frontend
curl http://localhost:4000

# Через nginx (HTTPS)
curl -k https://localhost
```

## 🛠️ Локальная разработка

### Установка зависимостей

```bash
npm install
```

### Запуск dev сервера

```bash
npm start
# или
ng serve
```

Приложение будет доступно на `http://localhost:4200`

### Сборка для production

```bash
npm run build
```

Собранные файлы будут в `dist/DonutsboxClient/`

### Запуск SSR сервера локально

```bash
npm run serve:ssr:DonutsboxClient
```

## 📝 Конфигурация

### Environment файлы

- `src/environments/environment.ts` - для разработки
- `src/environments/environment.prod.ts` - для production

В production используются относительные пути для API:
- `donutsboxApiBaseUrl: '/api'`
- `authApiBaseUrl: '/api/auth'`

Это позволяет nginx проксировать запросы к соответствующим backend сервисам.

## 🐛 Решение проблем

### Frontend не собирается

1. Проверьте версию Node.js (требуется Node 20+)
2. Очистите кэш: `rm -rf node_modules package-lock.json && npm install`
3. Проверьте логи: `sudo docker compose logs frontend`

### Frontend не отвечает

1. Проверьте статус: `sudo docker compose ps frontend`
2. Проверьте логи: `sudo docker compose logs frontend`
3. Убедитесь, что порт 4000 свободен

### Проблемы с API запросами

Убедитесь, что:
- Backend сервисы запущены
- Nginx правильно проксирует запросы
- Environment файлы используют правильные URL

## 📦 Структура

```
DonutsboxClient/
├── src/
│   ├── app/              # Основное приложение
│   │   ├── api/          # API клиенты (сгенерированные)
│   │   ├── core/          # Core модули (guards, services)
│   │   ├── features/      # Feature модули
│   │   └── shared/        # Shared компоненты
│   ├── environments/     # Environment конфигурация
│   ├── main.ts           # Точка входа (browser)
│   ├── main.server.ts    # Точка входа (server)
│   └── server.ts         # Express SSR сервер
├── public/               # Статические файлы
├── Dockerfile            # Docker конфигурация
└── package.json         # Зависимости
```

## 🔄 Обновление

После изменений в коде:

```bash
# Пересобрать и перезапустить
cd DonutsboxServer
sudo docker compose build --no-cache frontend
sudo docker compose up -d frontend
```
