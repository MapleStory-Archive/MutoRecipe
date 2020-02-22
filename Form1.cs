//  Copyright 2020 Jonguk Kim
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Image = SixLabors.ImageSharp.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

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

        public Form1()
        {
            for (var i = 0; i <= 15; i++)
            {
                Foods.Add(new Bitmap($"foods\\g_food_{i}.png"));
                Recipes.Add(new Bitmap($"recipes\\recipe_{i}.jpg"));
            }
            InitializeComponent();
            var initialStyle = Win32.GetWindowLong(this.Handle, -20);
            Win32.SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);
            this.Left = 0;
            this.Top = 0;
        }

        private double SearchImage(Bitmap screenBitmap, Bitmap searchBitmap)
        {
           
            using (var screenMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenBitmap))
            using (var findMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(searchBitmap))
            using (var res = screenMat.MatchTemplate(findMat, TemplateMatchModes.CCoeffNormed))
            {
                Cv2.MinMaxLoc(res, out _, out var val, out _, out _);
                return val;
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
            //var window = Win32.FindWindow("MapleStoryClass", "MapleStory"); 
            var window = Win32.FindWindow("Photo_Lightweight_Viewer", null);
            if (window != IntPtr.Zero)
            {
                label1.Visible = false;
                var graphics = Graphics.FromHwnd(window);
                var rect = Rectangle.Round(graphics.VisibleClipBounds);
                rect.Height /= 2;
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
                var rank = Enumerable.Range(0, 16).AsParallel()
                     .ToDictionary(i => i, i => SearchImage(imageBitmap, Foods[i]))
                     .OrderByDescending(i => i.Value)
                     .Select(a => a.Key).ToArray();
                pictureBox1.BackgroundImage = Recipes[rank[0]];
                Console.WriteLine($"{FoodNames[rank[0]]}가 발견됨");
             /* for (var i = 0; i < 16; i++)
              {
                  Console.Write($"{FoodNames[i]} : ");
                  Console.WriteLine(SearchImage(imageBitmap, Foods[i]));
              }*/
            }
            else
            {
                this.Left = 0;
                this.Top = 0;
                label1.Visible = true;
                pictureBox1.BackgroundImage = null;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //var window = Win32.FindWindow("MapleStoryClass", "MapleStory");
            var window = Win32.FindWindow("Photo_Lightweight_Viewer", null);
            if (window != IntPtr.Zero)
            {
                Win32.GetWindowRect(window, out var mapleRect);
                label1.Visible = false;
                this.Size = new System.Drawing.Size(428, 179);
                this.Top = mapleRect.Top + 5;
                this.Left = mapleRect.Left + 3;
            }
            else
            {
                this.Left = 0;
                this.Top = 0;
                label1.Visible = true;
                pictureBox1.BackgroundImage = null;
            }
        }
    }
}
