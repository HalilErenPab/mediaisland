# MEMORY — MediaIsland

## Güncel Durum

İlk sürüm çalışıyor (WPF, .NET 10). `kurulum.ps1` ile `%LOCALAPPDATA%\Programs\MediaIsland`
altına kurulu, Başlat menüsünde kısayolu var. Spotify ile uçtan uca test edildi. YouTube (tarayıcı),
fareyle açılma ve tam ekranda gizlenme henüz canlı denenmedi. Kişisel GitHub'a gidecek;
şirket şablonu/bilgi deposu kaydı kullanılmadı.

## Kayıtlar

### 2026-09-29 — Exe + kurulum
- **Ne:** `app.ico` (siyah ada + camgöbeği ekolayzır), `kurulum.ps1` (tek dosya publish →
  kullanıcı klasörü, Başlat menüsü kısayolu, başlat).
- **Neden:** Uygulama kapanınca `dotnet run`'a gerek kalmadan normal bir uygulama gibi açılsın.
- **Öğrenilenler:** Windows PowerShell 5.1 BOM'suz UTF-8 .ps1'deki Türkçe karakterleri bozar →
  `kurulum.ps1` UTF-8 BOM ile kaydedildi. Exe framework-dependent (24,5 MB; büyük kısmı WinRT
  projeksiyonu); başka makinede .NET 10 Desktop Runtime gerekir.

### 2026-09-29 — İlk sürüm
- **Ne:** Sağ altta Dynamic Island tarzı medya adası; kapak, önceki/oynat-duraklat/sonraki,
  ilerleme + sarma, kapaktan vurgu rengi, parça değişiminde 3,5 sn açılma.
- **Neden GSMTC:** Windows'un ortak medya API'si Spotify'ı da tarayıcıdaki YouTube'u da tek yerden
  veriyor; uygulamaya özel entegrasyon gerekmiyor.
- **Öğrenilenler:**
  - Şeffaf (`AllowsTransparency`) WPF penceresinde `RenderMode.SoftwareOnly` ölçümde çalarken
    CPU'yu %11 → %5,5 (tek çekirdek), belleği ~210 → ~75 MB düşürdü (GPU geri okuma maliyeti).
  - Küçükken görünmeyen öğeleri güncellemek bile layered pencereyi yeniden çizdiriyor —
    ilerleme yalnız açık hâlde güncelleniyor.
  - Kapak renginde neredeyse siyah JPEG pikselleri rastgele ton taşıyor; siyah-beyaz kapak mavi
    çıkıyordu → parlaklığı 0,2 altındaki pikseller yok sayılıyor.
  - Spotify'da "önceki" her zaman bir önceki parçaya dönmüyor (sıra/karıştırma davranışı).
  - .NET 10 dosya tabanlı `dotnet run x.cs` varsayılan AOT ile runtime pack indirmeye çalışıyor;
    deneme scriptlerinde `#:property PublishAot=false`.
