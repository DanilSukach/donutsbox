# 🚀 Быстрый старт Donutsbox Server

## Шаг 1: Генерация SSL сертификата

```bash
cd nginx
chmod +x generate-ssl-cert.sh
./generate-ssl-cert.sh
cd ..
```

**Если скрипт не работает:**
```bash
cd nginx
mkdir -p ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout ssl/selfsigned.key \
    -out ssl/selfsigned.crt \
    -subj "/C=RU/ST=State/L=City/O=Donutsbox/CN=localhost"
cd ..
```

## Шаг 2: Запуск всех сервисов

```bash
sudo docker compose up -d --build
```

## Шаг 3: Проверка

```bash
# Статус всех контейнеров
sudo docker compose ps

# Проверка health endpoints
curl -k https://localhost/health
curl http://localhost:7016/health
curl http://localhost:7133/health
```

## ✅ Готово!

- **Frontend (HTTPS):** https://localhost
- **Frontend (прямой доступ):** http://localhost:4000
- **Grafana:** http://localhost:3000 (admin/admin)
- **Prometheus:** http://localhost:9090
- **MinIO Console:** http://localhost:9001 (minio/minio123)

## 🐛 Проблемы?

1. **Nginx не запускается?** Проверьте SSL сертификат: `ls -la nginx/ssl/`
2. **Сервисы unhealthy?** Проверьте логи: `sudo docker compose logs <service-name>`
3. **Порты заняты?** Измените маппинг портов в `docker-compose.yml`

Подробнее: [README.md](README.md)

