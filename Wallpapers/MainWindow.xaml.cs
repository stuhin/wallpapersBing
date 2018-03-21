using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Wallpapers
{
    public class FileData
    {
        public BitmapImage Image { get; set; }
        public string Path { get; set; }
        public DateTime Created { get; set; }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(1, 0, 0);
            dispatcherTimer.Start();

            InitializeComponent();

            System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = Properties.Resources.Picture;
            notifyIcon.Visible = true;
            notifyIcon.Click += delegate (object sender, EventArgs args) 
            {
                if (WindowState == WindowState.Normal)
                {
                    Hide();
                    WindowState = WindowState.Minimized;
                }
                if (WindowState == WindowState.Minimized)
                {
                    Show();
                    WindowState = WindowState.Normal;
                }
            };

            Run();
        }

        private void SetDataGrid()
        {
            string currentpath = Directory.GetCurrentDirectory();
            Config config = Config.Get();
            List<FileData> fileDatas = new List<FileData>();
            List<FileInfo> fileInfos = Directory.GetFiles($"{currentpath}\\{config.desktop}").Select(p => new FileInfo(p)).OrderByDescending(f => f.CreationTime).Take(10).ToList();

            foreach (FileInfo file in fileInfos)
            {
                fileDatas.Add(new FileData() { Image = toBitmap(file.FullName), Path = file.FullName, Created = file.CreationTime });
            }

            if (fileDatas.Count > 0)
            {
                Wallpaper.Set(fileDatas.OrderByDescending(f => f.Created).First().Path);
            }

            dataGrid.ItemsSource = fileDatas.OrderByDescending(f => f.Created);
        }

        public static BitmapImage toBitmap(string path)
        {
            try
            {
                Byte[] value = File.ReadAllBytes(path);
                if (value != null && value is byte[])
                {
                    byte[] ByteArray = value as byte[];
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = new MemoryStream(ByteArray);
                    bmp.DecodePixelWidth = 250;
                    bmp.EndInit();
                    return bmp;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void SetDesktopBackground_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.Source).DataContext is FileData)
            {
                string path = ((FileData)((FrameworkElement)e.Source).DataContext).Path;
                Wallpaper.Set(path);
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }

            base.OnStateChanged(e);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Run();
            CommandManager.InvalidateRequerySuggested();
        }

        private void Run()
        {
            SaveFiles();
            SetDataGrid();
        }

        private void SaveFiles()
        {
            string currentpath = Directory.GetCurrentDirectory();
            Config config = Config.Get();

            WebClient client = new WebClient();

            List<Image> images = new List<Image>();
            foreach (string culture in config.cultures)
            {
                if (config.new_wallpapers)
                {
                    List<Image> images2 = GetBingWallpapers(client, culture, -1);
                    images.AddRange(images2);
                }
                if (config.arhive_wallpapers)
                {
                    List<Image> images2 = GetBingWallpapers(client, culture, 7);
                    images.AddRange(images2);
                }
            }

            foreach (string resolution in config.resolutions)
            {
                if (!Directory.Exists($"{currentpath}\\{resolution}"))
                {
                    Directory.CreateDirectory($"{currentpath}\\{resolution}");
                }
                List<string> files = Directory.GetFiles($"{currentpath}\\{resolution}").Select(s => s.Replace($"{currentpath}\\{resolution}\\", "").Split('_')[0]).ToList();

                SetLockScreenWallpapers(currentpath, resolution, files);
                SetBingWallpapers(client, images, files, currentpath, resolution);
            }

            if (!config.resolutions.Contains(config.desktop))
            {
                int width = int.Parse(config.desktop.Split('x')[0]);
                int height = int.Parse(config.desktop.Split('x')[1]);
                string resolution = config.resolutions.Where(r => int.Parse(r.Split('x')[0]) > width && int.Parse(r.Split('x')[1]) > height).OrderBy(r => r).FirstOrDefault();
                if(!string.IsNullOrEmpty(resolution))
                {
                    if (!Directory.Exists($"{currentpath}\\{config.desktop}"))
                    {
                        Directory.CreateDirectory($"{currentpath}\\{config.desktop}");
                    }

                    List<string> files = Directory.GetFiles($"{currentpath}\\{resolution}").ToList();
                    foreach (string file in files)
                    {
                        string path = file.Replace(resolution, config.desktop);
                        using (System.Drawing.Image image = System.Drawing.Image.FromFile(file))
                        using (System.Drawing.Image newImage = ScaleImage(image, width, height))
                        {
                            int x = newImage.Width == width ? 0 : (int)((newImage.Width - width) / 2);
                            int y = newImage.Height == height ? 0 : (int)((newImage.Height - height) / 2);
                            Crop(newImage, new Rectangle(x, y, width, height)).Save(file.Replace(resolution, config.desktop), ImageFormat.Jpeg);
                        }
                    }
                }
            }
        }

        private void SetLockScreenWallpapers(string currentpath, string resolution, List<string> files)
        {
            List<string> filesScreen = new List<string>();
            try
            {
                string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                filesScreen = Directory.GetFiles($"{userPath}\\AppData\\Local\\Packages\\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\\LocalState\\Assets").ToList();
            }
            catch
            {

            }

            foreach (string file in filesScreen)
            {
                try
                {
                    System.Drawing.Image imgInput = System.Drawing.Image.FromFile(file);
                    if (resolution == $"{imgInput.Width}x{imgInput.Height}")
                    {
                        string[] nameArray = file.Split('\\');
                        string name = nameArray[nameArray.Length - 1] + ".jpg";
                        if (!files.Contains(name))
                        {
                            string path = $"{currentpath}\\{resolution}\\{name}";
                            File.Copy(file, path);
                        }
                    }
                }
                catch
                {

                }
            }
        }
        private List<Image> GetBingWallpapers(WebClient client, string culture, int idx)
        {
            try
            {
                string json = client.DownloadString($"https://www.bing.com/HPImageArchive.aspx?format=js&idx={idx}&n=8&mkt={culture}");
                ImageJson imageJson = new JavaScriptSerializer().Deserialize<ImageJson>(json);
                return imageJson.images;
            }
            catch
            {
                return new List<Image>();
            }
        }

        private void SetBingWallpapers(WebClient client, List<Image> images, List<string> files, string currentpath, string resolution)
        {
            try
            {
                foreach (string urlbase in images.Select(i => i.urlbase).Distinct())
                {
                    string[] urlbaseArray = urlbase.Split('/');
                    string namebase = urlbaseArray[urlbaseArray.Length - 1];
                    string url = $"https://www.bing.com{urlbase}_{resolution}.jpg";
                    string path = $"{currentpath}\\{resolution}\\{namebase}_{resolution}.jpg";
                    string name = namebase.Split('_')[0];
                    if (!files.Contains(name))
                    {
                        client.DownloadFile(url, path);
                        files.Add(name);
                    }
                }
            }
            catch
            {

            }
        }

        public static System.Drawing.Image Crop(System.Drawing.Image image, Rectangle selection)
        {
            Bitmap bmp = image as Bitmap;
            Bitmap cropBmp = bmp.Clone(selection, bmp.PixelFormat);
            image.Dispose();
            return cropBmp;
        }

        public static System.Drawing.Image ScaleImage(System.Drawing.Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            double ratio = Math.Max(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }
    }
}
