using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Storage.Streams;

namespace MediaIsland;

/// <summary>Kapak görselini yükler ve adanın vurgu rengini kapaktan çıkarır.</summary>
static class CoverArt
{
    /// <summary>Gri tonlu ya da kapaksız parçalarda kullanılan sıcak beyaz.</summary>
    public static readonly Color FallbackAccent = Color.FromRgb(0xE6, 0xE1, 0xD6);

    public static async Task<BitmapSource> LoadAsync(IRandomAccessStreamReference thumbnail)
    {
        using var source = await thumbnail.OpenReadAsync();
        var buffer = new MemoryStream();
        await source.AsStreamForRead().CopyToAsync(buffer);
        buffer.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = buffer;
        image.EndInit();
        image.Freeze();
        return TrimLetterbox(image);
    }

    /// <summary>
    /// Kapağın baskın canlı tonu: tonlar 12 dilime ayrılır, doygunluk²×parlaklık ağırlığı en yüksek
    /// dilimin ortalaması alınır ve siyah adada okunacak parlaklığa çekilir. Gri tonlu kapakta FallbackAccent.
    /// </summary>
    public static Color Accent(BitmapSource cover)
    {
        var small = new TransformedBitmap(cover, new ScaleTransform(32.0 / cover.PixelWidth, 32.0 / cover.PixelHeight));
        var px = Pixels(small, out var w, out var h);

        var weight = new double[12];
        var r = new double[12];
        var g = new double[12];
        var b = new double[12];
        for (int i = 0; i < w * h * 4; i += 4)
        {
            var (hue, sat, val) = ToHsv(px[i + 2], px[i + 1], px[i]);
            // Neredeyse siyah piksellerin tonu JPEG gürültüsüdür (siyah kapak mavi çıkmasın).
            if (val < 0.2) continue;
            var wt = sat * sat * val;
            int bin = (int)(hue / 30) % 12;
            weight[bin] += wt;
            r[bin] += px[i + 2] * wt;
            g[bin] += px[i + 1] * wt;
            b[bin] += px[i] * wt;
        }

        int best = Array.IndexOf(weight, weight.Max());
        if (weight[best] < w * h * 0.01)
            return FallbackAccent;

        var (bh, bs, bv) = ToHsv(r[best] / weight[best], g[best] / weight[best], b[best] / weight[best]);
        return FromHsv(bh, Math.Clamp(bs, 0.35, 0.8), Math.Clamp(bv, 0.8, 0.95));
    }

    /// <summary>YouTube küçük resimleri 4:3 kalıpta gelir; 16:9 videonun üst/alt siyah bantlarını kırpar.</summary>
    static BitmapSource TrimLetterbox(BitmapSource image)
    {
        var px = Pixels(image, out var w, out var h);

        bool IsBlackRow(int y)
        {
            for (int x = 0; x < w; x += 2)
            {
                int i = (y * w + x) * 4;
                if (px[i] > 24 || px[i + 1] > 24 || px[i + 2] > 24)
                    return false;
            }
            return true;
        }

        int top = 0, bottom = 0;
        while (top < h / 4 && IsBlackRow(top)) top++;
        while (bottom < h / 4 && IsBlackRow(h - 1 - bottom)) bottom++;
        if (top < h / 20 || Math.Abs(top - bottom) > 4)
            return image;

        var cropped = new CroppedBitmap(image, new Int32Rect(0, top, w, h - top - bottom));
        cropped.Freeze();
        return cropped;
    }

    static byte[] Pixels(BitmapSource source, out int width, out int height)
    {
        var bgra = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        width = bgra.PixelWidth;
        height = bgra.PixelHeight;
        var px = new byte[width * height * 4];
        bgra.CopyPixels(px, width * 4, 0);
        return px;
    }

    static (double Hue, double Sat, double Val) ToHsv(double r, double g, double b)
    {
        r /= 255; g /= 255; b /= 255;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), delta = max - min;
        double hue = delta == 0 ? 0
            : max == r ? 60 * (((g - b) / delta + 6) % 6)
            : max == g ? 60 * ((b - r) / delta + 2)
            : 60 * ((r - g) / delta + 4);
        return (hue, max == 0 ? 0 : delta / max, max);
    }

    static Color FromHsv(double hue, double sat, double val)
    {
        double c = val * sat, x = c * (1 - Math.Abs(hue / 60 % 2 - 1)), m = val - c;
        var (r, g, b) = (int)(hue / 60) switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }
}
