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

if [ ! -e "$data_path/conf/options-ssl-nginx.conf" ] || [ ! -e "$data_path/conf/ssl-dhparams.pem" ]; then
  echo "### Скачивание рекомендуемых TLS параметров ..."
  mkdir -p "$data_path/conf"
  curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf > "$data_path/conf/options-ssl-nginx.conf"
  curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem > "$data_path/conf/ssl-dhparams.pem"
  echo "✓ TLS параметры скачаны в $data_path/conf/"
  echo
fi

echo "### Создание фиктивного сертификата для ${domains[*]} ..."
path="/etc/letsencrypt/live/$domain"
mkdir -p "$data_path/conf/live/$domain"
docker compose --env-file ../config.env run --rm --entrypoint "\
  openssl req -x509 -nodes -newkey rsa:$rsa_key_size -days 1\
    -keyout '$path/privkey.pem' \
    -out '$path/fullchain.pem' \
    -subj '/CN=localhost'" certbot
echo

echo "### Запуск nginx ..."
docker compose --env-file ../config.env up --force-recreate -d nginx
echo

echo "### Удаление фиктивного сертификата для ${domains[*]} ..."
docker compose --env-file ../config.env run --rm --entrypoint "\
  rm -Rf /etc/letsencrypt/live/$domain && \
  rm -Rf /etc/letsencrypt/archive/$domain && \
  rm -Rf /etc/letsencrypt/renewal/$domain.conf" certbot
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

echo "### Перезагрузка nginx ..."
docker compose --env-file ../config.env exec nginx nginx -s reload
