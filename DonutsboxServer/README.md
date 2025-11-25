# Donutsbox Server

Полнофункциональный серверный стек для Donutsbox приложения.

## 🚀 Быстрый старт

### Предварительные требования

- Docker и Docker Compose установлены
- OpenSSL (для генерации SSL сертификата)
- Минимум 4GB RAM
- Порты: 80, 443, 4000, 5432, 9092, 9000, 9001, 3000, 3100, 7016, 7133, 7207, 9090

### Шаг 1: Генерация SSL сертификата

**Важно:** SSL сертификат необходим для работы nginx с HTTPS.

```bash
cd nginx
chmod +x generate-ssl-cert.sh
./generate-ssl-cert.sh
cd ..
```

Если скрипт не работает, создайте сертификат вручную:

```bash
cd nginx
mkdir -p ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout ssl/selfsigned.key \
    -out ssl/selfsigned.crt \
    -subj "/C=RU/ST=State/L=City/O=Donutsbox/CN=localhost"
cd ..
```

### Шаг 2: Запуск всех сервисов

```bash
# Из корневой директории DonutsboxServer
sudo docker compose up -d --build
```

### Шаг 3: Проверка статуса

```bash
# Проверить статус всех контейнеров
sudo docker compose ps

# Проверить логи
sudo docker compose logs -f

# Проверить конкретный сервис
sudo docker compose logs nginx
```

## 📋 Сервисы

### Frontend

- **Donutsbox Client** - `https://localhost` (через nginx) или `http://localhost:4000` (напрямую)
- Angular SSR приложение с проксированием API через nginx

### API Сервисы

- **Auth API** - `http://localhost:7016` или `https://localhost/api/auth/`
- **Donutsbox API** - `http://localhost:7133` или `https://localhost/api/`
- **Admin Service API** - `http://localhost:7207` или `https://localhost/api/admin/`
- **File Service API** - внутренний сервис (порт 8086)

### Инфраструктура

- **Nginx** - Reverse proxy с HTTPS: `https://localhost`
- **PostgreSQL** - База данных: `localhost:5432`
- **Kafka** - Message broker: `localhost:9092`
- **MinIO** - Object storage: `http://localhost:9000` (Console: `http://localhost:9001`)
- **Prometheus** - Метрики: `http://localhost:9090`
- **Loki** - Логи: `http://localhost:3100`
- **Grafana** - Мониторинг: `http://localhost:3000` (admin/admin)
- **Promtail** - Сбор логов

## 🔧 Конфигурация

### Переменные окружения

Основные настройки находятся в `docker-compose.yml`. Для изменения конфигурации:

1. Отредактируйте `docker-compose.yml`
2. Пересоберите и перезапустите:

```bash
sudo docker compose down
sudo docker compose up -d --build
```

### База данных

- **Host:** postgres (внутри Docker) или localhost:5432 (снаружи)
- **Database:** donutsboxdb
- **User:** donuts
- **Password:** donutspw

### MinIO

- **Endpoint:** http://localhost:9000
- **Console:** http://localhost:9001
- **Access Key:** minio
- **Secret Key:** minio123

### Kafka

- **Bootstrap Servers:** localhost:9092
- **Topics:**
  - `video.uploaded`
  - `video.processed`

## 🔍 Проверка работы

### Health Checks

```bash
# Frontend
curl http://localhost:4000

# Nginx
curl -k https://localhost/health

# Auth API
curl http://localhost:7016/health

# Donutsbox API
curl http://localhost:7133/health

# Admin Service API
curl http://localhost:7207/health
```

### Проверка через браузер

- **Frontend (через Nginx):** https://localhost (будет предупреждение о self-signed сертификате)
- **Frontend (напрямую):** http://localhost:4000
- **Grafana:** http://localhost:3000 (admin/admin)
- **Prometheus:** http://localhost:9090
- **MinIO Console:** http://localhost:9001 (minio/minio123)
- **API через Nginx:** https://localhost/api/ (будет предупреждение о self-signed сертификате)

## 🛠️ Управление

### Остановка всех сервисов

```bash
sudo docker compose down
```

### Остановка с удалением volumes (⚠ удалит данные!)

```bash
sudo docker compose down -v
```

### Перезапуск конкретного сервиса

```bash
sudo docker compose restart nginx
sudo docker compose restart auth-api
```

### Просмотр логов

```bash
# Все логи
sudo docker compose logs -f

# Конкретный сервис
sudo docker compose logs -f nginx
sudo docker compose logs -f auth-api

# Последние 100 строк
sudo docker compose logs --tail=100 nginx
```

### Пересборка образов

```bash
# Пересборка всех образов
sudo docker compose build --no-cache
sudo docker compose up -d

# Пересборка только frontend
sudo docker compose build --no-cache frontend
sudo docker compose up -d frontend
```

## 🔐 SSL Сертификат

Используется self-signed сертификат для разработки. Браузер будет показывать предупреждение о безопасности - это нормально.

**Для продакшена:**
- Используйте реальный домен
- Настройте Let's Encrypt (см. `nginx/README.md`)

## 🐛 Решение проблем

### Nginx не запускается

1. Проверьте наличие SSL сертификата:
   ```bash
   ls -la nginx/ssl/
   ```

2. Если сертификата нет, создайте его (см. Шаг 1)

3. Проверьте логи:
   ```bash
   sudo docker compose logs nginx
   ```

### Сервисы unhealthy

1. Проверьте логи проблемного сервиса:
   ```bash
   sudo docker compose logs <service-name>
   ```

2. Проверьте зависимости (PostgreSQL, Kafka, MinIO):
   ```bash
   sudo docker compose ps
   ```

3. Убедитесь, что все зависимости запущены и healthy

### Проблемы с портами

Если порт занят, измените маппинг в `docker-compose.yml`:

```yaml
ports:
  - "НОВЫЙ_ПОРТ:ВНУТРЕННИЙ_ПОРТ"
```

## 📁 Структура проекта

```
donutsbox/
├── DonutsboxServer/       # Backend сервисы
│   ├── Auth.Api/          # Сервис аутентификации
│   ├── Donutsbox.Api/     # Основной API
│   ├── Admin.Service.Api/ # Админский API
│   ├── File.Service.Api/  # Сервис обработки файлов
│   ├── Donutsbox.Domain/  # Доменная модель
│   ├── nginx/             # Nginx конфигурация
│   │   ├── conf.d/        # Конфигурация nginx
│   │   ├── ssl/           # SSL сертификаты
│   │   └── generate-ssl-cert.sh
│   ├── monitoring/        # Конфигурация мониторинга
│   │   ├── prometheus/
│   │   ├── loki/
│   │   ├── promtail/
│   │   └── grafana/
│   └── docker-compose.yml # Основной compose файл
└── DonutsboxClient/       # Frontend (Angular SSR)
    ├── src/
    ├── Dockerfile
    └── package.json
```

## 🔄 Обновление

```bash
# Остановить сервисы
sudo docker compose down

# Обновить код (git pull)

# Пересобрать и запустить
sudo docker compose up -d --build
```

## 📝 Примечания

- Все пароли и ключи в `docker-compose.yml` - для разработки
- Для продакшена используйте переменные окружения и секреты
- Self-signed SSL сертификат действителен 365 дней
- Данные PostgreSQL, Kafka, MinIO сохраняются в Docker volumes

## 🆘 Поддержка

При возникновении проблем:
1. Проверьте логи: `sudo docker compose logs`
2. Проверьте статус: `sudo docker compose ps`
3. Проверьте ресурсы: `docker stats`
4. Убедитесь, что все порты свободны: `netstat -tulpn | grep LISTEN`

