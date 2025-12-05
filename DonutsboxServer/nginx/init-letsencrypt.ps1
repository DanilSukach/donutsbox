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

Write-Host "### Конфигурация:" -ForegroundColor Cyan
Write-Host "  Домен: $domain" -ForegroundColor White
Write-Host "  Email: $(if ($email) { $email } else { 'не указан' })" -ForegroundColor White
Write-Host "  IP сервера: $(if ($hostIp) { $hostIp } else { 'не указан' })" -ForegroundColor White
Write-Host ""

if (Test-Path $dataPath) {
    $domainsStr = $domains -join ", "
    $decision = Read-Host "Существующие данные найдены для $domainsStr. Продолжить и заменить существующий сертификат? (y/N)"
    if ($decision -ne "Y" -and $decision -ne "y") {
        exit
    }
}

if (-not (Test-Path "$dataPath\conf\options-ssl-nginx.conf") -or -not (Test-Path "$dataPath\conf\ssl-dhparams.pem")) {
    Write-Host "### Скачивание рекомендуемых TLS параметров ..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path "$dataPath\conf" | Out-Null
    
    # Скачиваем файлы
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf" -OutFile "$dataPath\conf\options-ssl-nginx.conf"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem" -OutFile "$dataPath\conf\ssl-dhparams.pem"
    Write-Host "✓ TLS параметры скачаны в $dataPath\conf\" -ForegroundColor Green
    Write-Host ""
}

$domainsStr = $domains -join ", "
Write-Host "### Создание фиктивного сертификата для $domainsStr ..." -ForegroundColor Cyan
$mainDomain = $domains[0]
$path = "/etc/letsencrypt/live/$mainDomain"
New-Item -ItemType Directory -Force -Path "$dataPath\conf\live\$mainDomain" | Out-Null

docker compose --env-file ..\config.env run --rm --entrypoint "openssl req -x509 -nodes -newkey rsa:$rsaKeySize -days 1 -keyout '$path/privkey.pem' -out '$path/fullchain.pem' -subj '/CN=localhost'" certbot
Write-Host ""

Write-Host "### Запуск nginx ..." -ForegroundColor Cyan
docker compose --env-file ..\config.env up --force-recreate -d nginx
Write-Host ""

Write-Host "### Удаление фиктивного сертификата для $domainsStr ..." -ForegroundColor Cyan
docker compose --env-file ..\config.env run --rm --entrypoint "rm -Rf /etc/letsencrypt/live/$mainDomain && rm -Rf /etc/letsencrypt/archive/$mainDomain && rm -Rf /etc/letsencrypt/renewal/$mainDomain.conf" certbot
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

Write-Host "### Перезагрузка nginx ..." -ForegroundColor Cyan
docker compose --env-file ..\config.env exec nginx nginx -s reload
Write-Host ""

Write-Host "### Готово! SSL сертификат получен для $domainsStr" -ForegroundColor Green
