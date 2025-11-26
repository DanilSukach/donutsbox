#!/bin/bash

# Скрипт для генерации self-signed SSL сертификата
# Используется для разработки без домена

# Получаем директорию скрипта
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SSL_DIR="$SCRIPT_DIR/ssl"

echo "Генерация self-signed SSL сертификата..."
echo "Директория: $SSL_DIR"

# Проверяем наличие OpenSSL
if ! command -v openssl &> /dev/null; then
    echo "✗ Ошибка: OpenSSL не установлен"
    echo "  Установите OpenSSL:"
    echo "    Ubuntu/Debian: sudo apt-get install openssl"
    echo "    CentOS/RHEL: sudo yum install openssl"
    exit 1
fi

# Создаем директорию для сертификатов
mkdir -p "$SSL_DIR"

# Генерируем self-signed сертификат
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout "$SSL_DIR/selfsigned.key" \
    -out "$SSL_DIR/selfsigned.crt" \
    -subj "/C=RU/ST=State/L=City/O=Donutsbox/CN=localhost"

if [ $? -eq 0 ]; then
    # Устанавливаем правильные права
    chmod 644 "$SSL_DIR/selfsigned.crt"
    chmod 600 "$SSL_DIR/selfsigned.key"
    
    echo ""
    echo "✓ Self-signed сертификат успешно создан!"
    echo "  - $SSL_DIR/selfsigned.crt"
    echo "  - $SSL_DIR/selfsigned.key"
    echo ""
    echo "⚠ ВНИМАНИЕ: Это self-signed сертификат для разработки!"
    echo "  Браузер будет показывать предупреждение о безопасности."
    echo "  Это нормально для разработки без домена."
    echo ""
    echo "Теперь можно запустить: docker compose up -d"
else
    echo "✗ Ошибка при создании сертификата"
    exit 1
fi

