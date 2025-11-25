#!/bin/bash

# Скрипт для генерации self-signed SSL сертификата
# Используется для разработки без домена

echo "Генерация self-signed SSL сертификата..."

# Создаем директорию для сертификатов
mkdir -p ssl

# Генерируем self-signed сертификат
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout ssl/selfsigned.key \
    -out ssl/selfsigned.crt \
    -subj "/C=RU/ST=State/L=City/O=Donutsbox/CN=localhost" \
    2>/dev/null

if [ $? -eq 0 ]; then
    echo "✓ Self-signed сертификат успешно создан в ssl/"
    echo "  - ssl/selfsigned.crt"
    echo "  - ssl/selfsigned.key"
    echo ""
    echo "⚠ ВНИМАНИЕ: Это self-signed сертификат для разработки!"
    echo "  Браузер будет показывать предупреждение о безопасности."
    echo "  Это нормально для разработки без домена."
else
    echo "✗ Ошибка при создании сертификата"
    echo "  Убедитесь, что OpenSSL установлен"
    exit 1
fi

