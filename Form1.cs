using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Image = SixLabors.ImageSharp.Image;

namespace MutoRecipe
{
    public partial class Form1 : Form
    {
        private readonly List<Bitmap> Foods = new List<Bitmap>();
        private readonly List<Bitmap> Recipes = new List<Bitmap>();
        private readonly string[] FoodNames =
        {
            "헉튀김", "앗볶읍", "이런면", "저런찜", "허허말이", "호호탕", "크헉구이", "으악샐러드", "낄낄볶음밥", "깔깔만두", "휴피자", "하빵", "오잉피클", "큭큭죽", "흑흑화채",
            "엉엉순대"
        };

        private readonly object locker = new object();

        public Form1()
        {
            for (var i = 0; i <= 15; i++)
            {
                Foods.Add(new Bitmap($"foods\\g_food_{i}.png"));
                Recipes.Add(new Bitmap($"recipes\\recipe_{i}.jpg"));
            }
            InitializeComponent();
            var initialStype = Win32.GetWindowLong(this.Handle, -20);
            Win32.SetWindowLong(this.Handle, -20, initialStype | 0x80000 | 0x20);
            this.Left = 0;
            this.Top = 0;
        }

        private bool SearchImage(Bitmap screenBitmap, Bitmap sarchBitmap)
        {
            using (var ScreenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenBitmap))
            using (var FindMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(sarchBitmap))
            using (var res = ScreenMat.MatchTemplate(FindMat, TemplateMatchModes.CCoeffNormed))
            {
                double minval, maxval;
                OpenCvSharp.Point minloc, maxloc;
                Cv2.MinMaxLoc(res, out minval, out maxval, out minloc, out maxloc);
                Console.WriteLine(maxval);
                return maxval >= 0.55d;
            }
        }

        private Bitmap ToBitmap<TPixel>(Image<TPixel> image) where TPixel : struct, IPixel<TPixel>
        {
            using (var memoryStream = new MemoryStream())
            {
                var imageEncoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(PngFormat.Instance);
                image.Save(memoryStream, imageEncoder);

                memoryStream.Seek(0, SeekOrigin.Begin);

                return new Bitmap(memoryStream);
            }
        }

        private Image<TPixel> ToImageSharpImage<TPixel>(Bitmap bitmap) where TPixel : struct, IPixel<TPixel>
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);

                memoryStream.Seek(0, SeekOrigin.Begin);

                return Image.Load<TPixel>(memoryStream);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var window = Win32.FindWindow("MapleStoryClass", "MapleStory");
            //   var window = Win32.FindWindow("Photo_Lightweight_Viewer", null);
            if (window != IntPtr.Zero)
            {
                label1.Visible = false;
                var graphics = Graphics.FromHwnd(window);
                var rect = Rectangle.Round(graphics.VisibleClipBounds);
                rect.Height /= 3;
                var bmp = new Bitmap(rect.Width, rect.Height);
                using (var gr = Graphics.FromImage(bmp))
                {
                    var hdc = gr.GetHdc();
                    Win32.PrintWindow(window, hdc, 1);
                    gr.ReleaseHdc(hdc);
                }

                pictureBox1.BackgroundImage = bmp;
                var image = ToImageSharpImage<Rgba32>(bmp);
                image.Mutate(data => data.Grayscale());
                var imageBitmap = ToBitmap(image);
                var detectedIndex = 0;
                Parallel.For(0, 16, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (i, state) =>
                   {
                       lock (locker)
                       {
                           if (SearchImage(imageBitmap, Foods[i]))
                           {
                               detectedIndex = i;
                               state.Break();
                           }
                       }
                   });
                pictureBox1.BackgroundImage = Recipes[detectedIndex];
                MessageBox.Show($"{FoodNames[detectedIndex]}가 발견됨", "MutoRecipe");
            }
            else
            {
                label1.Visible = true;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            var window = Win32.FindWindow("MapleStoryClass", "MapleStory");
            //var window = Win32.FindWindow("Photo_Lightweight_Viewer", null);
            if (window != IntPtr.Zero)
            {
                Win32.RECT mapleRect;
                Win32.GetWindowRect(window, out mapleRect);
                label1.Visible = false;
                this.Size = new System.Drawing.Size(428, 179);
                this.Top = mapleRect.Top + 5;
                this.Left = mapleRect.Left + 3;
            }
            else
            {
                label1.Visible = true;
            }
        }
    }
}
