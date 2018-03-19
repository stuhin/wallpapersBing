using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

namespace Wallpapers
{
    public class Config
    {
        public bool new_wallpapers { get; set; }
        public bool arhive_wallpapers { get; set; }
        public bool lockscreen_to_wallpapers { get; set; }
        public string desktop { get; set; }
        public string[] resolutions { get; set; }
        public string[] cultures { get; set; }

        public static Config Get()
        {
            if (!File.Exists("config.json"))
            {
                Config config = new Config()
                {
                    new_wallpapers = true,
                    arhive_wallpapers = true,
                    lockscreen_to_wallpapers = true,
                    desktop = "1920x1080",
                    resolutions = new string[] { "1920x1080", "1080x1920"},
                    cultures = new string[] { "en-US", "fr-FR", "de-DE", "ru-RU", "es-AR", "en-AU", "de-AT", "nl-BE", "fr-BE", "pt-BR", "en-CA", "fr-CA", "zh-HK", "en-IN", "en-ID", "it-IT", "ja-JP", "ko-KR", "en-MY", "es-MX", "nl-NL", "nb-NO", "zh-CN", "pl-PL", "ar-SA", "en-ZA", "es-ES", "sv-SE", "fr-CH", "de-CH", "zh-TW", "tr-TR", "en-GB", "es-US" }
                };
                string json = new JavaScriptSerializer().Serialize(config);
                File.WriteAllText("config.json", json.Replace(",",",\r\n"));
            }
            return new JavaScriptSerializer().Deserialize<Config>(File.ReadAllText("config.json"));
        }
    }

    public class ImageJson
    {
        public List<Image> images { get; set; }
    }

    public class Image
    {
        public string startdate { get; set; }
        public string fullstartdate { get; set; }
        public string enddate { get; set; }
        public string url { get; set; }
        public string urlbase { get; set; }
        public string copyright { get; set; }
    }

    public sealed class Wallpaper
    {
        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public static void Set(string path)
        {
            Stream s = File.OpenRead(path);

            System.Drawing.Image img = System.Drawing.Image.FromStream(s);
            string tempPath = Path.Combine(Path.GetTempPath(), "wallpaper.bmp");
            img.Save(tempPath, System.Drawing.Imaging.ImageFormat.Bmp);

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

            key.SetValue(@"WallpaperStyle", "2");
            key.SetValue(@"TileWallpaper", "0");

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, tempPath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}
