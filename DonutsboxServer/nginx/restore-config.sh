#!/bin/bash

# Скрипт для восстановления основной конфигурации nginx после получения SSL сертификата

cd "$(dirname "$0")"

echo "### Восстановление конфигурации nginx ..."

# Проверяем, есть ли временная конфигурация
if grep -q "Temporary LetsEncrypt config" ./conf.d/default.conf 2>/dev/null; then
  echo "⚠ Обнаружена временная конфигурация"
  
  # Проверяем наличие backup
  if [ -f "./conf.d/default.conf.backup" ]; then
    echo "✓ Найден backup файл, восстанавливаем..."
    cp ./conf.d/default.conf.backup ./conf.d/default.conf
    echo "✓ Конфигурация восстановлена из backup"
  else
    echo "⚠ Backup файл не найден"
    echo "Создаем правильную конфигурацию..."
    
    # Создаем правильную конфигурацию
    cat > ./conf.d/default.conf << 'EOF'
# HTTP server - для Let's Encrypt валидации и редиректа на HTTPS
server {
    listen 80;
    listen [::]:80;
    server_name donutsbox.ru www.donutsbox.ru;

    # Let's Encrypt challenge для получения сертификата
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    # Редирект всего остального на HTTPS
    location / {
        return 301 https://$host$request_uri;
    }
}

# HTTPS server с Let's Encrypt сертификатом
server {
    listen 443 ssl;
    listen [::]:443 ssl;
    http2 on;
    server_name donutsbox.ru www.donutsbox.ru;

    # Let's Encrypt SSL сертификаты
    ssl_certificate /etc/letsencrypt/live/donutsbox.ru/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/donutsbox.ru/privkey.pem;

    # SSL настройки безопасности
    # Используем встроенные настройки для работы без файлов certbot при инициализации
    ssl_session_cache shared:le_nginx_SSL:10m;
    ssl_session_timeout 1440m;
    ssl_session_tickets off;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_prefer_server_ciphers off;
    ssl_ciphers "ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:DHE-RSA-AES128-GCM-SHA256:DHE-RSA-AES256-GCM-SHA384";
    
    # DH параметры (опционально, если файл существует)
    # ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Логирование
    access_log /var/log/nginx/access.log;
    error_log /var/log/nginx/error.log;

    # Увеличение лимитов для загрузки файлов (до 10GB)
    client_max_body_size 10G;
    client_body_buffer_size 1M;

    # Health check endpoint (должен быть первым)
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }

    # ============================================
    # API Services Routing
    # Порядок важен: от специфичного к общему
    # ============================================

    # 1. Auth API: /api/auth/api/Auth/... -> /api/Auth/...
    location /api/auth/ {
        # Убираем /api/auth/ префикс, оставляя остальной путь
        rewrite ^/api/auth/(.*)$ /$1 break;
        proxy_pass http://auth.api:8080;
        include /etc/nginx/conf.d/proxy-common.conf;
    }

    # 2. Admin Service API: /api/admin/... -> /api/admin/...
    location /api/admin/ {
        proxy_pass http://admin.service.api:8084;
        include /etc/nginx/conf.d/proxy-common.conf;
    }

    # 3. Donutsbox API (main): /api/main/... -> /...
    location /api/main/ {
        # Убираем /api/main/ префикс, оставляя остальной путь как есть
        # /api/main/api/Files/images/avatar -> /api/Files/images/avatar
        # /api/main/api/hubs/comments -> /api/hubs/comments
        rewrite ^/api/main/(.*)$ /$1 break;
        proxy_pass http://donutsbox.api:8082;
        include /etc/nginx/conf.d/proxy-common.conf;
        
        # Специальные настройки для SignalR WebSocket
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }

    # 3.5. Специальный location для загрузки больших файлов
    location ~ ^/api/(Files/upload|CreatorPost/upload-images|Files/images/post) {
        proxy_pass http://donutsbox.api:8082;
        
        # Настройки для больших файлов (до 10GB)
        client_max_body_size 10G;
        client_body_buffer_size 1M;
        
        # Отключаем буферизацию для стриминга
        proxy_request_buffering off;
        proxy_buffering off;
        
        # Увеличенные таймауты для загрузки
        proxy_read_timeout 600s;
        proxy_connect_timeout 600s;
        proxy_send_timeout 600s;
        
        # Стандартные прокси заголовки
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
    # ============================================
    # External Services
    # ============================================

    # MinIO Console: /minio-console/... -> minio:9001/...
    # MinIO Console работает на корневом пути, поэтому проксируем без rewrite
    location /minio-console/ {
        proxy_pass http://minio:9001/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Port $server_port;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 300s;
        proxy_connect_timeout 75s;
        client_max_body_size 10G;
    }
    
    # MinIO Console (корневой путь для доступа к консоли)
    location = /minio-console {
        return 301 /minio-console/;
    }

    # MinIO API: /minio/... -> minio:9000/... (убираем /minio/)
    location /minio/ {
        rewrite ^/minio/(.*)$ /$1 break;
        proxy_pass http://minio:9000;
        proxy_http_version 1.1;
        # Важно: передаем оригинальный Host для валидации presigned URL
        # MinIO проверяет подпись на основе Host заголовка
        proxy_set_header Host minio:9000;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Port $server_port;
        proxy_redirect off;
        proxy_read_timeout 300s;
        proxy_connect_timeout 75s;
        client_max_body_size 10G;
        client_body_buffer_size 1M;
    }

    # ============================================
    # Frontend (должен быть последним)
    # ============================================
    location / {
        proxy_pass http://frontend:4000;
        include /etc/nginx/conf.d/proxy-common.conf;
    }
}
EOF
    echo "✓ Правильная конфигурация создана"
  fi
else
  echo "✓ Конфигурация уже правильная"
fi

echo ""
echo "### Проверка конфигурации nginx ..."
cd ..
if docker compose --env-file config.env exec nginx nginx -t 2>&1 | grep -q "successful"; then
  echo "✓ Конфигурация nginx валидна"
else
  echo "❌ Ошибка в конфигурации nginx:"
  docker compose --env-file config.env exec nginx nginx -t
  exit 1
fi

echo ""
echo "### Перезагрузка nginx ..."
docker compose --env-file config.env exec nginx nginx -s reload

if [ $? -eq 0 ]; then
  echo "✓ Nginx перезагружен"
else
  echo "⚠ Не удалось перезагрузить, пробуем перезапустить контейнер..."
  docker compose --env-file config.env restart nginx
  sleep 3
  if docker compose --env-file config.env ps nginx | grep -q "Up"; then
    echo "✓ Nginx перезапущен"
  else
    echo "❌ Ошибка: nginx не запустился"
    exit 1
  fi
fi

echo ""
echo "### Готово! Конфигурация восстановлена"
