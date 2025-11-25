# Настройка Nginx с Self-Signed SSL сертификатом

Эта конфигурация использует self-signed SSL сертификат для HTTPS без необходимости домена.

## 🚀 Быстрый старт

### 1. Сгенерируйте SSL сертификат

**Для Linux/Mac/WSL:**
```bash
chmod +x generate-ssl-cert.sh
./generate-ssl-cert.sh
```

**Для Windows (PowerShell):**
```powershell
# Убедитесь, что OpenSSL установлен, или используйте WSL
# Если OpenSSL установлен:
mkdir ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout ssl\selfsigned.key -out ssl\selfsigned.crt -subj "/C=RU/ST=State/L=City/O=Donutsbox/CN=localhost"
```

**Вручную (если скрипт не работает):**
```bash
mkdir -p ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout ssl/selfsigned.key \
    -out ssl/selfsigned.crt \
    -subj "/C=RU/ST=State/L=City/O=Donutsbox/CN=localhost"
```

### 2. Запустите контейнеры

```bash
# Из корневой директории DonutsboxServer
cd ..
sudo docker compose up -d
```

### 3. Проверьте работу

```bash
# Проверка HTTP редиректа
curl -I http://localhost/health

# Проверка HTTPS (будет предупреждение о self-signed сертификате)
curl -k https://localhost/health

# Проверка API
curl -k https://localhost/api/auth/health
```

## ⚙️ Конфигурация

### Структура файлов

- `conf.d/default.conf` - основная конфигурация nginx с HTTPS
- `ssl/` - директория для SSL сертификатов (не коммитится в git)
- `generate-ssl-cert.sh` - скрипт генерации сертификата (Linux/Mac)

### Проксирование

Nginx проксирует запросы на следующие сервисы:

- `/api/auth/` → `http://auth.api:8080/`
- `/api/` → `http://donutsbox.api:8082/`
- `/api/admin/` → `http://admin.service.api:8084/`
- `/health` → Health check endpoint

### SSL Настройки

- **Протоколы:** TLSv1.2, TLSv1.3
- **Cipher Suites:** Современные безопасные наборы
- **HSTS:** Включен (max-age=31536000)
- **HTTP/2:** Включен

## ⚠️ Важно!

**Self-signed сертификат** - это только для разработки!

- Браузер будет показывать предупреждение о безопасности
- Это нормально для разработки без домена
- Для продакшена нужен реальный домен и Let's Encrypt сертификат

## 🔍 Проверка и отладка

### Проверка конфигурации nginx

```bash
sudo docker compose exec nginx nginx -t
```

### Перезагрузка nginx

```bash
sudo docker compose exec nginx nginx -s reload
```

### Просмотр логов

```bash
sudo docker compose logs nginx
sudo docker compose logs -f nginx  # в реальном времени
```

### Проверка SSL сертификата

```bash
# Проверить наличие файлов
ls -la ssl/

# Должны быть:
# - ssl/selfsigned.crt
# - ssl/selfsigned.key
```

## 🐛 Решение проблем

### Ошибка: "cannot load certificate"

**Причина:** SSL сертификат не создан или находится не в том месте.

**Решение:**
```bash
cd nginx
./generate-ssl-cert.sh
# Или создайте вручную (см. выше)
cd ..
sudo docker compose restart nginx
```

### Ошибка: "deprecated http2 directive"

**Причина:** Использован старый синтаксис.

**Решение:** Уже исправлено в `default.conf`. Если видите предупреждение, убедитесь, что используете последнюю версию конфигурации.

### Nginx перезапускается постоянно

**Причина:** Ошибка в конфигурации или отсутствует SSL сертификат.

**Решение:**
1. Проверьте логи: `sudo docker compose logs nginx`
2. Проверьте конфигурацию: `sudo docker compose exec nginx nginx -t`
3. Убедитесь, что SSL сертификат создан

## 📝 API Endpoints

После запуска доступны через HTTPS:

- `https://localhost/api/auth/` - Auth API
- `https://localhost/api/` - Donutsbox API  
- `https://localhost/api/admin/` - Admin Service API
- `https://localhost/health` - Health check

HTTP (порт 80) автоматически редиректит на HTTPS (порт 443).

## 🔄 Обновление сертификата

Сертификат действителен 365 дней. Для обновления:

```bash
cd nginx
./generate-ssl-cert.sh
cd ..
sudo docker compose restart nginx
```
