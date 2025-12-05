#!/bin/bash

# Скрипт для первоначального получения Let's Encrypt SSL сертификата для donutsbox.ru

if ! [ -x "$(command -v docker compose)" ]; then
  echo 'Error: docker compose is not installed.' >&2
  exit 1
fi

# Загружаем переменные из config.env
if [ -f "../config.env" ]; then
  set -a
  source ../config.env
  set +a
else
  echo "Ошибка: файл config.env не найден в родительской директории" >&2
  exit 1
fi

# Получаем значения из config.env или используем значения по умолчанию
domain="${DOMAIN:-donutsbox.ru}"
domains=("$domain" "www.$domain")
rsa_key_size=4096
data_path="./nginx/certbot"
email="${LETSENCRYPT_EMAIL:-}"
host_ip="${HOST_IP:-}"
staging=0 # Установите в 1 для тестирования (staging окружение)

echo "### Конфигурация:"
echo "  Домен: $domain"
echo "  Email: ${email:-не указан}"
echo "  IP сервера: ${host_ip:-не указан}"
echo ""

if [ -d "$data_path" ]; then
  read -p "Существующие данные найдены для ${domains[*]}. Продолжить и заменить существующий сертификат? (y/N) " decision
  if [ "$decision" != "Y" ] && [ "$decision" != "y" ]; then
    exit
  fi
fi

# TLS параметры будут скачаны после получения сертификата

# Создаем временную HTTP-only конфигурацию для получения сертификата
echo "### Создание временной HTTP-only конфигурации nginx ..."
temp_conf="./conf.d/default-temp.conf"
cat > "$temp_conf" << 'EOF'
# Временная HTTP-only конфигурация для получения Let's Encrypt сертификата
server {
    listen 80;
    listen [::]:80;
    server_name _;

    # Let's Encrypt challenge для получения сертификата
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    # Health check endpoint
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }

    # Временный ответ для остальных запросов
    location / {
        return 503 "SSL certificate is being obtained. Please wait...\n";
        add_header Content-Type text/plain;
    }
}
EOF
echo "✓ Временная конфигурация создана"
echo

# Переименовываем основную конфигурацию, если она существует
if [ -f "./conf.d/default.conf" ]; then
  echo "### Сохранение основной конфигурации ..."
  mv ./conf.d/default.conf ./conf.d/default.conf.backup
  echo "✓ Основная конфигурация сохранена как default.conf.backup"
fi

# Используем временную конфигурацию
mv "$temp_conf" ./conf.d/default.conf
echo "✓ Временная конфигурация активирована"
echo

echo "### Запуск nginx с временной конфигурацией ..."
docker compose --env-file ../config.env up -d nginx
echo

# Ждем, пока nginx запустится
echo "### Ожидание запуска nginx ..."
sleep 5

# Проверяем, что nginx запущен
if ! docker compose --env-file ../config.env ps nginx | grep -q "Up"; then
  echo "Ошибка: nginx не запустился. Проверьте логи: docker compose --env-file ../config.env logs nginx" >&2
  exit 1
fi
echo "✓ Nginx запущен"
echo

echo "### Запрос реального сертификата для ${domains[*]} ..."
# Выберите email
case "$email" in
  "") email_option="--register-unsafely-without-email" ;;
  *) email_option="--email $email" ;;
esac

# Включить staging режим, если нужно
if [ $staging != "0" ]; then staging_arg="--staging"; fi

domain_args=""
for d in "${domains[@]}"; do
  domain_args="$domain_args -d $d"
done

# Запрос сертификата
docker compose --env-file ../config.env run --rm --entrypoint "\
  certbot certonly --webroot -w /var/www/certbot \
    $staging_arg \
    $email_option \
    $domain_args \
    --rsa-key-size $rsa_key_size \
    --agree-tos \
    --force-renewal" certbot
echo

echo "### Скачивание рекомендуемых TLS параметров в certbot volume ..."
# Скачиваем файлы напрямую в certbot volume через контейнер (после получения сертификата)
docker compose --env-file ../config.env run --rm --entrypoint "\
  mkdir -p /etc/letsencrypt && \
  curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf -o /etc/letsencrypt/options-ssl-nginx.conf && \
  curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem -o /etc/letsencrypt/ssl-dhparams.pem && \
  ls -la /etc/letsencrypt/options-ssl-nginx.conf /etc/letsencrypt/ssl-dhparams.pem && \
  echo 'TLS параметры скачаны'" certbot
echo "✓ TLS параметры скачаны в certbot volume"
echo

echo "### Восстановление основной конфигурации nginx ..."
if [ -f "./conf.d/default.conf.backup" ]; then
  mv ./conf.d/default.conf.backup ./conf.d/default.conf
  echo "✓ Основная конфигурация восстановлена"
else
  echo "⚠ Предупреждение: файл default.conf.backup не найден. Возможно, конфигурация уже была восстановлена."
fi
echo

echo "### Перезагрузка nginx с полной конфигурацией ..."
# Проверяем конфигурацию перед перезагрузкой
if docker compose --env-file ../config.env exec nginx nginx -t 2>&1 | grep -q "successful"; then
  docker compose --env-file ../config.env exec nginx nginx -s reload
  echo "✓ Nginx перезагружен с полной конфигурацией"
else
  echo "⚠ Ошибка в конфигурации nginx. Пробуем перезапустить контейнер..."
  docker compose --env-file ../config.env restart nginx
  sleep 3
  if docker compose --env-file ../config.env ps nginx | grep -q "Up"; then
    echo "✓ Nginx перезапущен"
  else
    echo "❌ Ошибка: nginx не запустился. Проверьте логи: docker compose --env-file ../config.env logs nginx"
    exit 1
  fi
fi
echo

echo "### Готово! SSL сертификат получен для ${domains[*]}"
