# Donutsbox

Полнофункциональное приложение для работы с видео контентом.

## 🚀 Быстрый запуск

### Серверная часть (DonutsboxServer)

```bash
cd DonutsboxServer

# 1. Создать SSL сертификат для nginx
cd nginx
chmod +x generate-ssl-cert.sh
./generate-ssl-cert.sh
cd ..

# 2. Запустить все сервисы
sudo docker compose up -d --build

# 3. Проверить статус
sudo docker compose ps
```

Подробные инструкции: [DonutsboxServer/README.md](DonutsboxServer/README.md)

### Клиентская часть (DonutsboxClient)

**В Docker (рекомендуется):**
```bash
cd DonutsboxServer
sudo docker compose up -d --build frontend
```

**Локальная разработка:**
```bash
cd DonutsboxClient
npm install
npm start  # или ng serve
```

## 📋 Структура проекта

- `DonutsboxServer/` - Backend сервисы (API, база данных, инфраструктура)
- `DonutsboxClient/` - Frontend приложение (Angular)

## 🔧 Режим разработки

Для запуска в режиме разработки:

```bash
cd DonutsboxServer
sudo docker-compose -f docker-compose.dev.yml up --build -d
