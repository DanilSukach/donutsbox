# PowerShell скрипт для первоначального получения Let's Encrypt SSL сертификата для donutsbox.ru

$rsaKeySize = 4096
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

$domainsStr = $domains -join ", "

Write-Host "### Конфигурация:" -ForegroundColor Cyan
Write-Host "  Домен: $domain" -ForegroundColor White
Write-Host "  Email: $(if ($email) { $email } else { 'не указан' })" -ForegroundColor White
Write-Host ""

# Создаем временную HTTP-only конфигурацию для получения сертификата
Write-Host "### Создание временной HTTP-only конфигурации nginx ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path ".\conf.d" | Out-Null

$tempConfContent = @"
server {
    listen 80;
    server_name _;

    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    location / {
        return 200 "Temporary LetsEncrypt config`n";
    }
}
"@
Set-Content -Path ".\conf.d\default.conf" -Value $tempConfContent

# Сохраняем основную конфигурацию, если она существует
if (Test-Path ".\conf.d\default.conf.backup") {
    Write-Host "⚠ Основная конфигурация уже сохранена" -ForegroundColor Yellow
} else {
    if ((Test-Path ".\conf.d\default.conf") -and ((Get-Content ".\conf.d\default.conf" -Raw) -notmatch "Temporary LetsEncrypt config")) {
        Copy-Item -Path ".\conf.d\default.conf" -Destination ".\conf.d\default.conf.backup" -Force
        Write-Host "✓ Основная конфигурация сохранена" -ForegroundColor Green
    }
}

Write-Host "✓ Временная конфигурация создана" -ForegroundColor Green
Write-Host ""

Write-Host "### Запуск nginx с временной конфигурацией ..." -ForegroundColor Cyan
docker compose --env-file ..\config.env up -d nginx
Write-Host ""

Start-Sleep -Seconds 5

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
$certbotCommand = "certonly --webroot -w /var/www/certbot $stagingArg $emailOption $domainArgs --rsa-key-size $rsaKeySize --agree-tos --force-renewal"
docker compose --env-file ..\config.env run --rm certbot $certbotCommand
Write-Host ""

Write-Host "### Скачивание TLS параметров на хост ..." -ForegroundColor Cyan
$certbotEtc = ".\certbot"
New-Item -ItemType Directory -Force -Path $certbotEtc | Out-Null

try {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf" -OutFile "$certbotEtc\options-ssl-nginx.conf"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem" -OutFile "$certbotEtc\ssl-dhparams.pem"
    
    if ((Test-Path "$certbotEtc\options-ssl-nginx.conf") -and (Test-Path "$certbotEtc\ssl-dhparams.pem")) {
        Write-Host "✓ TLS параметры скачаны в $certbotEtc\" -ForegroundColor Green
    } else {
        Write-Host "Ошибка: не удалось скачать TLS параметры" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Ошибка при скачивании: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

Write-Host "### Копирование TLS параметров в certbot volume ..." -ForegroundColor Cyan
$absolutePath = (Resolve-Path $certbotEtc).Path
docker compose --env-file ..\config.env run --rm --entrypoint "mkdir -p /etc/letsencrypt && cp /tmp/options-ssl-nginx.conf /etc/letsencrypt/ && cp /tmp/ssl-dhparams.pem /etc/letsencrypt/ && ls -la /etc/letsencrypt/options-ssl-nginx.conf /etc/letsencrypt/ssl-dhparams.pem" -v "${absolutePath}:/tmp:ro" certbot
Write-Host "✓ TLS параметры скопированы в certbot volume" -ForegroundColor Green
Write-Host ""

Write-Host "### Восстановление основной конфигурации nginx ..." -ForegroundColor Cyan
if (Test-Path ".\conf.d\default.conf.backup") {
    Copy-Item -Path ".\conf.d\default.conf.backup" -Destination ".\conf.d\default.conf" -Force
    Write-Host "✓ Основная конфигурация восстановлена" -ForegroundColor Green
} else {
    Write-Host "⚠ Предупреждение: файл default.conf.backup не найден" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "### Перезапуск nginx ..." -ForegroundColor Cyan
docker compose --env-file ..\config.env restart nginx
Start-Sleep -Seconds 3

$nginxStatus = docker compose --env-file ..\config.env ps nginx
if ($nginxStatus -match "Up") {
    Write-Host "✓ Nginx перезапущен" -ForegroundColor Green
} else {
    Write-Host "❌ Ошибка: nginx не запустился. Проверьте логи: docker compose --env-file ..\config.env logs nginx" -ForegroundColor Red
    exit 1
}
Write-Host ""

Write-Host "### Готово! SSL сертификат получен для $domainsStr" -ForegroundColor Green
