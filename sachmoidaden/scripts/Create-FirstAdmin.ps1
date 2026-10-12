$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    $env:BOOTSTRAP_ADMIN_NAME = Read-Host 'Tên hiển thị Admin'
    $env:BOOTSTRAP_ADMIN_EMAIL = Read-Host 'Email Admin'
    $securePassword = Read-Host 'Mật khẩu (8–100 ký tự)' -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $env:BOOTSTRAP_ADMIN_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    dotnet run --project sachmoidaden.csproj --no-launch-profile -- --create-admin
    if ($LASTEXITCODE -ne 0) { throw 'Chưa tạo được Admin. Kiểm tra kết nối SQL và thông báo phía trên.' }
}
finally {
    Remove-Item Env:BOOTSTRAP_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:BOOTSTRAP_ADMIN_EMAIL -ErrorAction SilentlyContinue
    Remove-Item Env:BOOTSTRAP_ADMIN_NAME -ErrorAction SilentlyContinue
    if ($securePassword) { $securePassword.Dispose() }
    Pop-Location
}
