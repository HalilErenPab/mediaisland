# MediaIsland'ı derleyip kullanıcı klasörüne kurar ve Başlat menüsüne kısayol ekler.
# Kod değiştikten sonra tekrar çalıştırmak yeterli; yönetici izni gerekmez.
#   powershell -ExecutionPolicy Bypass -File kurulum.ps1
$ErrorActionPreference = 'Stop'

$target = Join-Path $env:LOCALAPPDATA 'Programs\MediaIsland'
$exe = Join-Path $target 'MediaIsland.exe'

# Çalışan kopya exe'yi kilitler; kapatıp çıkmasını bekle.
Get-Process MediaIsland -ErrorAction SilentlyContinue | ForEach-Object { $_.Kill(); $_.WaitForExit() }

dotnet publish "$PSScriptRoot\MediaIsland.csproj" -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -p:DebugType=none -o $target
if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız; yukarıdaki hata çıktısına bak.' }

$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Programs')) 'MediaIsland.lnk'))
$link.TargetPath = $exe
$link.WorkingDirectory = $target
$link.Description = 'Şu an çalan müzik için Dynamic Island tarzı ada'
$link.Save()

Start-Process $exe
"Kuruldu ve başlatıldı: $exe"
"Başlat menüsünde 'MediaIsland' diye aratarak açabilirsin."
