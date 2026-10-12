$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    dotnet run --no-launch-profile -- --seed-demo
    if ($LASTEXITCODE -ne 0) { throw 'Không tạo được dữ liệu mẫu. Kiểm tra database V3 và connection string.' }
} finally {
    Pop-Location
}
