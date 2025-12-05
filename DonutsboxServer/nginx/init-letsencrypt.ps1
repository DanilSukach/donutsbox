# PowerShell скрипт для первоначального получения Let's Encrypt SSL сертификата для donutsbox.ru

$rsaKeySize = 4096
$dataPath = ".\nginx\certbot"
$staging = 0 # Установите в 1 для тестирования (staging окружение)

Write-Host "### Проверка docker compose..." -ForegroundColor Cyan
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Ошибка: docker не установлен." -ForegroundColor Red
    exit 1
}

# Загружаем переменные из config.env
$configPath = "..\config.env"
if (Test-Path $configPath) {
    Get-Content $configPath | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]*?)\s*=\s*(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            if ($value -match '^"(.*)"$') {
                $value = $matches[1]
            }
            Set-Variable -Name $key -Value $value -Scope Script
        }
    }
} else {
    Write-Host "Ошибка: файл config.env не найден в родительской директории" -ForegroundColor Red
    exit 1
}

# Получаем значения из config.env или используем значения по умолчанию
$domain = if ($DOMAIN) { $DOMAIN } else { "donutsbox.ru" }
$domains = @($domain, "www.$domain")
$email = if ($LETSENCRYPT_EMAIL) { $LETSENCRYPT_EMAIL } else { "" }
$hostIp = if ($HOST_IP) { $HOST_IP } else { "" }

$domainsStr = $domains -join ", "

Write-Host "### Конфигурация:" -ForegroundColor Cyan
Write-Host "  Домен: $domain" -ForegroundColor White
Write-Host "  Email: $(if ($email) { $email } else { 'не указан' })" -ForegroundColor White
Write-Host "  IP сервера: $(if ($hostIp) { $hostIp } else { 'не указан' })" -ForegroundColor White
Write-Host ""

if (Test-Path $dataPath) {
    $decision = Read-Host "Существующие данные найдены для $domainsStr. Продолжить и заменить существующий сертификат? (y/N)"
    if ($decision -ne "Y" -and $decision -ne "y") {
        exit
    }
}

# TLS параметры будут скачаны после получения сертификата

# Создаем временную HTTP-only конфигурацию для получения сертификата
Write-Host "### Создание временной HTTP-only конфигурации nginx ..." -ForegroundColor Cyan
$tempConf = ".\conf.d\default-temp.conf"
$tempConfContent = @"
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
        return 200 "healthy`n";
        add_header Content-Type text/plain;
    }

    # Временный ответ для остальных запросов
    location / {
        return 503 "SSL certificate is being obtained. Please wait...`n";
        add_header Content-Type text/plain;
    }
}
"@
Set-Content -Path $tempConf -Value $tempConfContent
Write-Host "✓ Временная конфигурация создана" -ForegroundColor Green
Write-Host ""

# Переименовываем основную конфигурацию, если она существует
if (Test-Path ".\conf.d\default.conf") {
    Write-Host "### Сохранение основной конфигурации ..." -ForegroundColor Cyan
    Move-Item -Path ".\conf.d\default.conf" -Destination ".\conf.d\default.conf.backup" -Force
    Write-Host "✓ Основная конфигурация сохранена как default.conf.backup" -ForegroundColor Green
}

# Используем временную конфигурацию
Move-Item -Path $tempConf -Destination ".\conf.d\default.conf" -Force
Write-Host "✓ Временная конфигурация активирована" -ForegroundColor Green
Write-Host ""

Write-Host "### Запуск nginx с временной конфигурацией ..." -ForegroundColor Cyan
docker compose --env-file ..\config.env up -d nginx
Write-Host ""

# Ждем, пока nginx запустится
Write-Host "### Ожидание запуска nginx ..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

# Проверяем, что nginx запущен
$nginxStatus = docker compose --env-file ..\config.env ps nginx
if ($nginxStatus -notmatch "Up") {
    Write-Host "Ошибка: nginx не запустился. Проверьте логи: docker compose --env-file ..\config.env logs nginx" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Nginx запущен" -ForegroundColor Green
Write-Host ""

Write-Host "### Запрос реального сертификата для $domainsStr ..." -ForegroundColor Cyan

# Выбор email
if ([string]::IsNullOrEmpty($email)) {
    $emailOption = "--register-unsafely-without-email"
} else {
    $emailOption = "--email $email"
}

# Включить staging режим, если нужно
if ($staging -ne 0) {
    $stagingArg = "--staging"
} else {
    $stagingArg = ""
}

# Формируем аргументы доменов
$domainArgs = ""
foreach ($d in $domains) {
    $domainArgs += " -d $d"
}

# Запрос сертификата
$certbotCommand = "certbot certonly --webroot -w /var/www/certbot $stagingArg $emailOption $domainArgs --rsa-key-size $rsaKeySize --agree-tos --force-renewal"
docker compose --env-file ..\config.env run --rm --entrypoint $certbotCommand certbot
Write-Host ""

Write-Host "### Скачивание рекомендуемых TLS параметров в certbot volume ..." -ForegroundColor Cyan
# Скачиваем файлы напрямую в certbot volume через контейнер (после получения сертификата)
$downloadCommand = "mkdir -p /etc/letsencrypt && curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf -o /etc/letsencrypt/options-ssl-nginx.conf && curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem -o /etc/letsencrypt/ssl-dhparams.pem && ls -la /etc/letsencrypt/options-ssl-nginx.conf /etc/letsencrypt/ssl-dhparams.pem && echo 'TLS параметры скачаны'"
docker compose --env-file ..\config.env run --rm --entrypoint $downloadCommand certbot
Write-Host "✓ TLS параметры скачаны в certbot volume" -ForegroundColor Green
Write-Host ""

Write-Host "### Восстановление основной конфигурации nginx ..." -ForegroundColor Cyan
if (Test-Path ".\conf.d\default.conf.backup") {
    Move-Item -Path ".\conf.d\default.conf.backup" -Destination ".\conf.d\default.conf" -Force
    Write-Host "✓ Основная конфигурация восстановлена" -ForegroundColor Green
} else {
    Write-Host "⚠ Предупреждение: файл default.conf.backup не найден. Возможно, конфигурация уже была восстановлена." -ForegroundColor Yellow
}
Write-Host ""

Write-Host "### Перезагрузка nginx с полной конфигурацией ..." -ForegroundColor Cyan
# Проверяем конфигурацию перед перезагрузкой
$testResult = docker compose --env-file ..\config.env exec nginx nginx -t 2>&1
if ($testResult -match "successful") {
    docker compose --env-file ..\config.env exec nginx nginx -s reload
    Write-Host "✓ Nginx перезагружен с полной конфигурацией" -ForegroundColor Green
} else {
    Write-Host "⚠ Ошибка в конфигурации nginx. Пробуем перезапустить контейнер..." -ForegroundColor Yellow
    docker compose --env-file ..\config.env restart nginx
    Start-Sleep -Seconds 3
    $nginxStatus = docker compose --env-file ..\config.env ps nginx
    if ($nginxStatus -match "Up") {
        Write-Host "✓ Nginx перезапущен" -ForegroundColor Green
    } else {
        Write-Host "❌ Ошибка: nginx не запустился. Проверьте логи: docker compose --env-file ..\config.env logs nginx" -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

Write-Host "### Готово! SSL сертификат получен для $domainsStr" -ForegroundColor Green
