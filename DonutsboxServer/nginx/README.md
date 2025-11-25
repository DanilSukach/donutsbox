# Настройка Nginx с Self-Signed SSL сертификатом

Эта конфигурация использует self-signed SSL сертификат для HTTPS без необходимости домена.

## Быстрый старт

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
openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout ssl/selfsigned.key -out ssl/selfsigned.crt -subj "/C=RU/ST=State/L=City/O=Donutsbox/CN=localhost"
```

### 2. Запустите контейнеры

```bash
cd ..
docker compose up -d
```

### 3. Проверьте работу

- HTTP (порт 80) автоматически редиректит на HTTPS
- HTTPS доступен на порту 443
- Все API endpoints работают через HTTPS

## Важно!

⚠ **Self-signed сертификат** - это только для разработки!

- Браузер будет показывать предупреждение о безопасности
- Это нормально для разработки без домена
- Для продакшена нужен реальный домен и Let's Encrypt сертификат

## Структура

- `conf.d/default.conf` - конфигурация nginx с HTTPS
- `ssl/` - директория для SSL сертификатов
- `generate-ssl-cert.sh` - скрипт генерации сертификата (Linux/Mac)
- `generate-ssl-cert.ps1` - скрипт генерации сертификата (Windows)

## API Endpoints

После запуска доступны:
- `https://localhost/api/auth/` - Auth API
- `https://localhost/api/` - Donutsbox API  
- `https://localhost/api/admin/` - Admin Service API
- `https://localhost/health` - Health check
