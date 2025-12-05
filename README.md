# Настройка Nginx с Let's Encrypt SSL сертификатом для donutsbox.ru

Эта конфигурация использует Let's Encrypt SSL сертификат для домена `donutsbox.ru` и `www.donutsbox.ru`.

## 🚀 Быстрый старт

### Предварительные требования

1. **Домен должен указывать на ваш сервер**
   - DNS A-запись для `donutsbox.ru` должна указывать на IP вашего сервера
   - DNS A-запись для `www.donutsbox.ru` (опционально, но рекомендуется)

2. **Порты 80 и 443 должны быть открыты**
   - Порт 80 нужен для HTTP-01 challenge Let's Encrypt
   - Порт 443 для HTTPS трафика

3. **Docker и Docker Compose установлены**

### 1. Настройка конфигурации

Перед получением SSL сертификата настройте переменные в файле `config.env` в корне `DonutsboxServer/`:

```bash
# Домен для SSL сертификата
DOMAIN=donutsbox.ru

# Email для уведомлений Let's Encrypt (опционально, но рекомендуется)
LETSENCRYPT_EMAIL=your-email@example.com

# IP адрес сервера (уже должен быть настроен)
HOST_IP=31.130.144.104
```

### 2. Получение SSL сертификата

**Для Linux/Mac/WSL:**
```bash
cd DonutsboxServer/nginx
chmod +x init-letsencrypt.sh
./init-letsencrypt.sh
```

**Для Windows (PowerShell):**
```powershell
cd DonutsboxServer\nginx
.\init-letsencrypt.ps1
```

**Важно:** Перед запуском скрипта убедитесь, что:
- Домен указан в `config.env` (переменная `DOMAIN`) и указывает на IP вашего сервера
- Порты 80 и 443 открыты в файрволе
- Docker Compose может запустить контейнеры
- Email указан в `config.env` (переменная `LETSENCRYPT_EMAIL`) для получения уведомлений от Let's Encrypt

**Как работает скрипт:**
1. Создает временную HTTP-only конфигурацию nginx (без SSL)
2. Запускает nginx с временной конфигурацией
3. Скачивает TLS параметры в certbot volume
4. Получает SSL сертификат от Let's Encrypt
5. Автоматически восстанавливает полную конфигурацию с HTTPS
6. Перезагружает nginx с полной конфигурацией

### 3. Запуск сервисов

```bash
# Из корневой директории DonutsboxServer
docker compose --env-file config.env up -d
```

### 4. Проверка работы

```bash
# Проверка HTTP редиректа
curl -I http://donutsbox.ru/health

# Проверка HTTPS
curl https://donutsbox.ru/health

# Проверка API
curl https://donutsbox.ru/api/auth/health
```

## ⚙️ Конфигурация

### Структура файлов

- `conf.d/default.conf` - основная конфигурация nginx с HTTPS для donutsbox.ru
- `init-letsencrypt.sh` / `init-letsencrypt.ps1` - скрипты для первоначального получения сертификата
- `ssl/` - директория для старых self-signed сертификатов (если использовались)

**Примечание:** Скрипты `init-letsencrypt.sh` и `init-letsencrypt.ps1`:
- Автоматически читают значения из `../config.env`:
  - `DOMAIN` - домен для SSL сертификата (по умолчанию: `donutsbox.ru`)
  - `LETSENCRYPT_EMAIL` - email для уведомлений Let's Encrypt (опционально)
  - `HOST_IP` - IP адрес сервера (используется для информации)
- Автоматически создают временную HTTP-only конфигурацию для получения сертификата
- Скачивают TLS параметры (options-ssl-nginx.conf и ssl-dhparams.pem) в certbot volume
- После получения сертификата автоматически восстанавливают полную конфигурацию с HTTPS

### Проксирование

Nginx проксирует запросы на следующие сервисы:

- `/api/auth/` → `http://auth.api:8080/` (убирает префикс `/api/auth/`)
- `/api/main/` → `http://donutsbox.api:8082/` (убирает префикс `/api/main/`)
- `/api/admin/` → `http://admin.service.api:8084/`
- `/minio/` → `http://minio:9000/` (убирает префикс `/minio/`)
- `/minio-console/` → `http://minio:9001/`
- `/health` → Health check endpoint
- `/` → Frontend (http://frontend:4000)

### SSL Настройки

- **Домен:** donutsbox.ru, www.donutsbox.ru
- **Протоколы:** TLSv1.2, TLSv1.3
- **Cipher Suites:** Современные безопасные наборы (рекомендации certbot)
- **HSTS:** Включен (max-age=31536000)
- **HTTP/2:** Включен
- **Автообновление:** Certbot автоматически обновляет сертификаты каждые 12 часов

## 🔄 Обновление сертификата

Certbot автоматически обновляет сертификаты. Контейнер `certbot` запускается в режиме `restart: "no"` и периодически проверяет необходимость обновления.

Для ручного обновления:
```bash
docker compose --env-file config.env run --rm certbot renew
docker compose --env-file config.env exec nginx nginx -s reload
```

## 🔍 Проверка и отладка

### Проверка конфигурации nginx

```bash
docker compose --env-file config.env exec nginx nginx -t
```

### Перезагрузка nginx

```bash
docker compose --env-file config.env exec nginx nginx -s reload
```

### Просмотр логов

```bash
# Логи nginx
docker compose --env-file config.env logs nginx
docker compose --env-file config.env logs -f nginx  # в реальном времени

# Логи certbot
docker compose --env-file config.env logs certbot
```

### Проверка SSL сертификата

```bash
# Проверить сертификат через openssl
openssl s_client -connect donutsbox.ru:443 -servername donutsbox.ru

# Или через curl
curl -vI https://donutsbox.ru

# Проверить срок действия сертификата
docker compose --env-file config.env exec certbot certbot certificates
```

### Проверка DNS

```bash
# Проверить, что домен указывает на правильный IP
nslookup donutsbox.ru
dig donutsbox.ru
```

## 🐛 Решение проблем

### Ошибка: "cannot load certificate"

**Причина:** SSL сертификат не получен или находится не в том месте.

**Решение:**
1. Убедитесь, что вы запустили `init-letsencrypt.sh` или `init-letsencrypt.ps1`
2. Проверьте, что домен указывает на ваш сервер
3. Проверьте логи: `docker compose --env-file config.env logs certbot`
4. Попробуйте получить сертификат в staging режиме (измените `staging=1` в скрипте)

### Ошибка: "Connection refused" при получении сертификата

**Причина:** Let's Encrypt не может подключиться к вашему серверу для валидации.

**Решение:**
1. Убедитесь, что порт 80 открыт и доступен из интернета
2. Проверьте, что nginx запущен: `docker compose --env-file config.env ps nginx`
3. Проверьте DNS: домен должен указывать на IP вашего сервера
4. Проверьте файрвол: `sudo ufw status` или `sudo firewall-cmd --list-all`

### Nginx не запускается

**Причина:** Ошибка в конфигурации или отсутствует SSL сертификат.

**Решение:**
1. Проверьте логи: `docker compose --env-file config.env logs nginx`
2. Проверьте конфигурацию: `docker compose --env-file config.env exec nginx nginx -t`
3. Убедитесь, что сертификат получен: `docker compose --env-file config.env exec certbot certbot certificates`
4. Если сертификата нет, запустите `init-letsencrypt.sh` снова

### Сертификат не обновляется автоматически

**Причина:** Контейнер certbot не запущен или не настроен правильно.

**Решение:**
1. Проверьте, что контейнер certbot существует: `docker compose --env-file config.env ps certbot`
2. Запустите вручную: `docker compose --env-file config.env up -d certbot`
3. Проверьте логи: `docker compose --env-file config.env logs certbot`

### Использование staging окружения для тестирования

Если вы хотите протестировать получение сертификата без ограничений Let's Encrypt:

1. Откройте `init-letsencrypt.sh` или `init-letsencrypt.ps1`
2. Измените `staging=0` на `staging=1`
3. Запустите скрипт
4. После успешного теста измените обратно на `staging=0` и получите реальный сертификат

## 📝 API Endpoints

После запуска доступны через HTTPS:

- `https://donutsbox.ru/api/auth/` - Auth API
- `https://donutsbox.ru/api/main/` - Donutsbox API (main)
- `https://donutsbox.ru/api/` - Donutsbox API (общий)
- `https://donutsbox.ru/api/admin/` - Admin Service API
- `https://donutsbox.ru/minio/` - MinIO API
- `https://donutsbox.ru/minio-console/` - MinIO Console
- `https://donutsbox.ru/health` - Health check
- `https://donutsbox.ru/` - Frontend

HTTP (порт 80) автоматически редиректит на HTTPS (порт 443), кроме пути `/.well-known/acme-challenge/` для Let's Encrypt валидации.

## 🔒 Безопасность

- Используются только современные и безопасные TLS протоколы
- HSTS включен для защиты от downgrade атак
- Security headers настроены для защиты от XSS, clickjacking и других атак
- Сертификаты автоматически обновляются через certbot

## 📚 Дополнительные ресурсы

- [Let's Encrypt документация](https://letsencrypt.org/docs/)
- [Certbot документация](https://eff-certbot.readthedocs.io/)
- [Nginx SSL настройки](https://nginx.org/en/docs/http/configuring_https_servers.html)
