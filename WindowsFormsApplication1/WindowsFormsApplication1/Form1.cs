using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SharpFont;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WindowsFormsApplication1
{
    public struct GlyphData
    {
        public byte[]       buffer;

        public int          width;
        public int          height;

        public int          pitch;
        public int          forward;

        public int          aX;
        public int          aY;

        public int          kX;
        public int          kY;

        public uint         charIndex;

        public GlyphMetrics metrics;
    }

    public partial class Form1 : Form
    {
        Bitmap bmp;
        Dictionary<char, GlyphData> charCache;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string hello = "The quick brown fox jumps over the lazy dog.";

            charCache = new Dictionary<char, GlyphData>();

            Library lib = new Library();
            Face fontFace = new Face(lib, "fonts/roboto-light.ttf");

            fontFace.SetCharSize(0, 30 * 64, 0, 96);
            
            int wX          = 0;
            int wY          = 0;
            int wyWB        = 0;

            int xB          = 0;

            int pX          = 0;
            int pY          = 0;

            uint current    = 0;
            uint previous   = 0;

            GlyphData       data;

            DateTime iTimeS = DateTime.Now;

            foreach (var item in hello)
            {
                current = fontFace.GetCharIndex(item);

                if (!charCache.ContainsKey(item))
                {
                    fontFace.LoadGlyph(current, LoadFlags.Default, LoadTarget.Normal);
                    fontFace.Glyph.RenderGlyph(RenderMode.Normal);

                    FTBitmap bitmap = fontFace.Glyph.Bitmap;

                    data = new GlyphData();

                    if (item != ' ')
                        data.buffer = bitmap.BufferData;

                    data.charIndex  = current;
                    data.height     = fontFace.Glyph.Metrics.Height / 64;
                    data.width      = fontFace.Glyph.Metrics.Width / 64;
                    data.metrics    = fontFace.Glyph.Metrics;
                    data.pitch      = fontFace.Glyph.BitmapTop;
                    data.forward    = fontFace.Glyph.BitmapLeft;
                    data.aX         = fontFace.Glyph.Metrics.HorizontalAdvance;
                    data.aY         = fontFace.Glyph.Metrics.VerticalAdvance;

                    charCache.Add(item, data);
                }
                else
                {
                    data = charCache[item];
                }

                wX     += data.aX / 64;

                if (fontFace.HasKerning && previous != 0 && current != 0)
                {
                    var kInfo = fontFace.GetKerning(previous, current, KerningMode.Default);

                    wX += kInfo.X / 64;

                    data.kX = kInfo.X / 64;
                    data.kY = kInfo.Y / 64;
                }

                wyWB    = Math.Max(data.pitch, wyWB);
                xB      = Math.Max(data.height - data.pitch, xB);
                
                wY      = wyWB + xB;

                previous = current;
            }

            DateTime iTimeF = DateTime.Now;

            Console.WriteLine("Time in letting freetype render the glyphs: {0} milliseconds.", (iTimeF - iTimeS).TotalMilliseconds);

            bmp = new Bitmap(wX, wY);

            int[] map = new int[wX * wY];

            for (int i = 0; i < map.Length; i++ )
            {
                map[i] = 0;
            }

            current     = 0;
            previous    = 0;

            DateTime timeS = DateTime.Now;

            for (int i = 0; i < hello.Length; i++)
            {
                var item = hello[i];

                current = fontFace.GetCharIndex(item);

                GlyphData pdata = charCache[item];

                for (int y = 0; y < pdata.height; y++)
                {
                    for (int x = 0; x < pdata.width; x++)
                    {
                        int fx = x + pX + pdata.forward;
                        int fy = wY - pdata.pitch + y - xB;

                        if (fy < 0 || fy >= wY)
                        {
                            Console.WriteLine("Overflow on fy! :(");
                            continue;
                        }

                        if (fx < 0 || fx >= wX)
                        {
                            Console.WriteLine("Overflow on fx! :(");
                            continue;
                        }

                        int fdata = pdata.buffer[y * pdata.width + x] + map[fy * wX + fx];

                        if (fdata >= 255)
                            fdata = 255;

                        map[fy * wX + fx] = (fdata << 24) | (0 << 16) | (0 << 8) | 0;
                    }
                }

                pX += pdata.aX / 64;

                if (fontFace.HasKerning && previous != 0 && current != 0)
                {
                    pX += pdata.kX;
                }

                previous = current;
            }

            BitmapData bData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            Marshal.Copy(map, 0, bData.Scan0, map.Length);

            bmp.UnlockBits(bData);

            map = null;

            DateTime timeF = DateTime.Now;

            Console.WriteLine("Time in mapping them pixels: {0} milliseconds.", (timeF- timeS).TotalMilliseconds);

            GC.Collect();

            ClientSize      = new System.Drawing.Size(wX, wY);
            StartPosition   = FormStartPosition.CenterScreen;

            BackColor       = Color.White;

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (bmp != null)
            {
                e.Graphics.DrawImage(bmp, Point.Empty);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            Invalidate();
        }
    }
}
