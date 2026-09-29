# MediaIsland

Windows masaüstü için Apple Dynamic Island tarzı "şu an çalıyor" adası. Sağ altta, görev
çubuğunun hemen üstünde küçük siyah bir hap olarak durur; fareyle üzerine gelince açılır.

- **Kaynaklar:** Spotify, Chrome/Edge/Firefox'taki YouTube ve Windows'a medya bilgisi bildiren
  her uygulama (Windows'un ortak medya API'si — GSMTC). Uygulamaya özel entegrasyon yok.
- **Küçük hâl:** kapak + kapak renginde ekolayzır (duraklatınca noktaya iner).
- **Açık hâl:** büyük kapak, kaynak / başlık / sanatçı, ilerleme çubuğu (tıklayarak sarma, kaynak
  destekliyorsa), önceki / oynat-duraklat / sonraki.
- Yeni parça başlayınca 3,5 sn kendiliğinden açılıp kapanır.
- Vurgu rengi her parçada kapaktan çıkarılır ve kapağın arkasından adaya taşar.
- Çalan bir şey yokken ve tam ekran video/oyun/sunum sırasında gizlenir.
- Alt+Tab'da görünmez, tıklanınca odağı çalıştığın uygulamadan almaz.
- Kapatmak için: adaya sağ tık → **MediaIsland'ı kapat**.

## Gereksinim

Windows 10 2004+ / Windows 11, .NET 10 SDK.

## Kurulum

```bash
powershell -ExecutionPolicy Bypass -File kurulum.ps1
```

Tek dosyalık `MediaIsland.exe`'yi `%LOCALAPPDATA%\Programs\MediaIsland` altına kurar, Başlat
menüsüne kısayol ekler ve uygulamayı başlatır. Yönetici izni istemez. Kod değişince aynı komutu
tekrar çalıştırmak yeterli (çalışan kopyayı kapatıp günceller). Exe, makinede .NET 10 Desktop
Runtime olmasını bekler.

Kapattıktan sonra tekrar açmak için Başlat menüsünde **MediaIsland** diye arat.

## Geliştirme

```bash
dotnet run
```

## Yapı

| Dosya | Görev |
|---|---|
| `MediaService.cs` | GSMTC oturumlarını izler, hangi oturumun gösterileceğini seçer, komutları iletir |
| `CoverArt.cs` | Kapağı yükler, YouTube'un siyah bantlarını kırpar, vurgu rengini çıkarır |
| `MainWindow.xaml(.cs)` | Ada arayüzü, genişleme animasyonu, ilerleme, görünürlük |
| `NativeMethods.cs` | Araç penceresi stili, tam ekran algılama |
| `kurulum.ps1` | Yayınla + kullanıcı klasörüne kur + Başlat menüsü kısayolu |
| `app.ico` | Uygulama ikonu (siyah ada + ekolayzır) |

## Bilinen sınırlar

- Videonun kendisi gösterilmez, yalnızca kapak/küçük resim.
- YouTube'da "önceki" genelde yalnız çalma listesinde etkin (tarayıcı öyle bildiriyor).
- Tarayıcıda kaynak adı "CHROME/EDGE" görünür; hangi siteden geldiğini API vermiyor.
- Ekolayzır gerçek ses seviyesini değil, animasyonu gösterir.
