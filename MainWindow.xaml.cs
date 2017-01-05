using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WaterWave
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //获取圆心为(0,0)半径为r的圆上点的偏移，算法为： a= amplitude*sin(2pi*r/cycle) , 折射偏移为, offset = a*sinθ1/sinθ2*sin(θ2-θ1)，sinθ1 = a/amplitude，水到空气折射关系sinθ1/sinθ2=3/4
        private double GetOffsetOnCycle(double r, double amplitude, double cycle, int waterdepth)
        {
            double k = amplitude * Math.PI * 2 / cycle * Math.Cos(Math.PI * 2 * r / cycle);
            double sino1 = Math.Abs(k) / Math.Sqrt(k * k + 1);
            double coso1 = Math.Sqrt(1 - sino1 * sino1);
            double sino2 = sino1 * 4 / 3;
            if (sino2 >= 1 || sino2 <= -1) return 0;
            double coso2 = Math.Sqrt(1 - sino2 * sino2);
            double tr = r % cycle;//abs(o1)<abs(o2)<90度，所以o1、o2肯定在同一象限
            double a = (waterdepth + amplitude) * Math.Sin(Math.PI * 2 * r / cycle);
            double offset = Math.Abs(a * 3 / 4 * (sino2 * coso1 - sino1 * coso2));
            if (tr > 0 && tr < cycle / 4 || tr > cycle * 3 / 4 && tr < cycle)//一四象限，offset为负
                offset = -offset;
            return offset;
        }

        private bool IsOnCycle(double r, double x, double y)
        {
            double d = Math.Sqrt(x * x + y * y);
            return Math.Abs(d - r) < 0.7;//圆上，小于 (根号2)/2
        }

        /// <summary>
        /// 模拟生成水纹，算法为： a= amplitude*sin(2pi*r/cycle) , 折射偏移为, offset = a*sinθ1/sinθ2*sin(θ2-θ1)，sinθ1 = a/amplitude，水到空气折射关系sinθ1/sinθ2=3/4
        /// </summary>
        /// <param name="pixelbuffer">图像数据</param>
        /// <param name="pixelwidth">图像像素宽度（像素为单位）</param>
        /// <param name="pixelheight">图像高度（像素为单位）</param>
        /// <param name="centerx">水纹中心x</param>
        /// <param name="centery">水纹中心y</param>
        /// <param name="amplitude">波动振幅</param>
        /// <param name="cycle">波动周期，标准正弦曲线通常为 cycle = 2pi*amplitude，可以以此为参考值</param>
        /// <param name="stopRadius">水纹的最大扩散范围半径</param>
        /// <param name="waterdepth">水深，通常大于amplitude，值越大像素偏移越大</param>
        private void Wave(byte[] pixelbuffer, int pixelwidth, int pixelheight, int centerx, int centery, int amplitude, int cycle, int stopRadius, int waterdepth)
        {
            for (int r = 1; r <= stopRadius; r++)
            {
                double offset = GetOffsetOnCycle(r, amplitude, cycle, waterdepth);
                if (Math.Abs((int)offset) < 1) continue;
                Point[] points = new Point[20 * (2 * r + 1)];
                int c = 0;
                for (int dx = -r; dx <= r; dx++) //按x轴取整遍历圆上的像素点
                {
                    if (dx + centerx < 0) { dx = -centerx - 1; continue; }
                    if (dx + centerx > pixelwidth) break;
                    int dy = (int)Math.Sqrt(r * r - dx * dx);
                    if (-dy + centery < 0 && dy + centery > pixelheight) continue;
                    points[c++] = new Point(dx, dy); //当前点，然后检查周围的四个点是否在圆上
                    //if (IsOnCycle(r, dx - 1, dy)) points[c++] = new Point(dx - 1, dy);
                    //if (IsOnCycle(r, dx, dy - 1)) points[c++] = new Point(dx, dy - 1);
                    //if (IsOnCycle(r, dx + 1, dy)) points[c++] = new Point(dx + 1, dy);
                    //if (IsOnCycle(r, dx, dy + 1)) points[c++] = new Point(dx, dy + 1);
                    points[c++] = new Point(dx, -dy);
                    //if (IsOnCycle(r, dx - 1, -dy)) points[c++] = new Point(dx - 1, -dy);
                    //if (IsOnCycle(r, dx, dy - 1)) points[c++] = new Point(dx, dy - 1);
                    //if (IsOnCycle(r, dx + 1, -dy)) points[c++] = new Point(dx + 1, -dy);
                    //if (IsOnCycle(r, dx, dy + 1)) points[c++] = new Point(dx, dy + 1);
                }
                for (int dy = -r; dy <= r; dy++) //按y轴取整遍历圆上的像素点
                {
                    if (dy + centery < 0) { dy = -centery - 1; continue; }
                    if (dy + centery > pixelheight) break;
                    int dx = (int)Math.Sqrt(r * r - dy * dy);
                    if (-dx + centerx < 0 && dx + centerx > pixelwidth) continue;
                    points[c++] = new Point(dx, dy);//当前点，然后检查周围的四个点是否在圆上
                    //if (IsOnCycle(r, dx - 1, dy)) points[c++] = new Point(dx - 1, dy);
                    //if (IsOnCycle(r, dx, dy - 1)) points[c++] = new Point(dx, dy - 1);
                    //if (IsOnCycle(r, dx + 1, dy)) points[c++] = new Point(dx + 1, dy);
                    //if (IsOnCycle(r, dx, dy + 1)) points[c++] = new Point(dx, dy + 1);
                    points[c++] = new Point(-dx, dy);
                    //if (IsOnCycle(r, -dx - 1, dy)) points[c++] = new Point(-dx - 1, dy);
                    //if (IsOnCycle(r, -dx, dy - 1)) points[c++] = new Point(-dx, dy - 1);
                    //if (IsOnCycle(r, -dx + 1, dy)) points[c++] = new Point(-dx + 1, dy);
                    //if (IsOnCycle(r, -dx, dy + 1)) points[c++] = new Point(-dx, dy + 1);
                }
                for (int i = 0; i < c; i++)
                {
                    int dx = (int)points[i].X;
                    int dy = (int)points[i].Y;
                    int offsetx = (int)(dx / (float)r * offset);
                    int offsety = (int)(dy / (float)r * offset);
                    if (Math.Abs(offsetx) < 1 && Math.Abs(offsety) < 1) continue;
                    if (centerx + dx + offsetx < 0 || centerx + dx + offsetx >= pixelwidth ||
                        centery + dy + offsety < 0 || centery + dy + offsety >= pixelheight) continue;

                    int sindex = ((centery + dy) * pixelwidth + (centerx + dx)) * 4;
                    int windex = ((centery + dy + offsety) * pixelwidth + (centerx + dx + offsetx)) * 4;//扩散点
                    if (sindex < 0 || sindex > pixelbuffer.Length - 4) continue;
                    pixelbuffer[windex] = pixelbuffer[sindex];
                    pixelbuffer[windex + 1] = pixelbuffer[sindex + 1];
                    pixelbuffer[windex + 2] = pixelbuffer[sindex + 2];
                }

                if (r % cycle == 0)//每经历一个周期振幅衰减一次
                {
                    if (amplitude > 16)
                        amplitude -= amplitude >> 4;//振幅衰减
                    else
                        amplitude--;
                }
                if (amplitude < 4) break;
            }
        }

        private void Button_WaterWave_Click(object sender, RoutedEventArgs e)
        {
            var source = Image_Source.Source as BitmapSource;
            int stride = source.PixelWidth * source.Format.BitsPerPixel / 8;
            if (stride % 4 != 0) stride = (stride / 4 + 1) * 4;//(跨度即每行的字节数，应该为4的整数倍)
            byte[] buffer = new byte[source.PixelHeight * stride];
            source.CopyPixels(buffer, stride, 0);

            Stopwatch watch = new Stopwatch();
            watch.Start();

            int amplitude = 32, cycle = 64;//cycle至少大于amplitude，否则能发生偏移的点会很少
            int waterdepth = amplitude+64;//水深一般大于amplitude
            int centerx = source.PixelWidth / 2, centery = source.PixelHeight / 2;
            int maxLen = (int)Math.Sqrt(Math.Pow(source.PixelWidth, 2) + Math.Pow(source.PixelHeight, 2)) / 2;
            Wave(buffer, source.PixelWidth, source.PixelHeight, centerx, centery, amplitude, cycle, maxLen, waterdepth);
            int i = 1;// 在周围增加四个波源
            Wave(buffer, source.PixelWidth, source.PixelHeight, centerx - i, centery, amplitude / 2, cycle / 2, maxLen, waterdepth);
            Wave(buffer, source.PixelWidth, source.PixelHeight, centerx, centery - i, amplitude / 2, cycle / 2, maxLen, waterdepth);
            Wave(buffer, source.PixelWidth, source.PixelHeight, centerx + i, centery, amplitude / 2, cycle / 2, maxLen, waterdepth);
            Wave(buffer, source.PixelWidth, source.PixelHeight, centerx, centery + i, amplitude / 2, cycle / 2, maxLen, waterdepth);
            
            var destionation = BitmapImage.Create(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, source.Format, source.Palette, buffer, stride);
            Image_Destination.Source = destionation;

            Text_CostTime.Text = "耗时：" + watch.ElapsedMilliseconds + "ms";
        }

    }
}
