# PowerShell скрипт для генерации self-signed SSL сертификата
# Используется для разработки без домена

Write-Host "Генерация self-signed SSL сертификата..." -ForegroundColor Cyan

# Создаем директорию для сертификатов
New-Item -ItemType Directory -Force -Path "nginx\ssl" | Out-Null

# Генерируем self-signed сертификат
$cert = New-SelfSignedCertificate `
    -DnsName "localhost", "127.0.0.1" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(1)

# Экспортируем сертификат и приватный ключ
$pwd = ConvertTo-SecureString -String "temp" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "nginx\ssl\temp.pfx" -Password $pwd | Out-Null

# Конвертируем PFX в PEM формат (требуется OpenSSL или можно использовать другой метод)
# Для Windows можно использовать другой подход

Write-Host "✓ Сертификат создан в Cert:\CurrentUser\My" -ForegroundColor Green
Write-Host ""
Write-Host "⚠ ВНИМАНИЕ: Для Windows рекомендуется использовать OpenSSL для создания .crt и .key файлов" -ForegroundColor Yellow
Write-Host "  Или используйте WSL/Linux версию скрипта generate-ssl-cert.sh" -ForegroundColor Yellow

