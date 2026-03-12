using commClassLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private List<GeoPoint> _geoPoints = new List<GeoPoint>();
        private MapPainter _mapPainter;
        private ClickDetector _clickDetector;

        public Form1()
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = false;
            _mapPainter = new MapPainter(panel1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 初始不加载任何文件，等待用户点击按钮五加载
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            if (_geoPoints.Count >= 3)
            {
                double area = GeoDataProcessor.CalculateArea(_geoPoints);
                MessageBox.Show($"{area:F2} 平方千米");
            }
            else
            {
                MessageBox.Show("至少需要3个坐标点才能计算面积。");
            }
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            _mapPainter.StartPainting();
        }

        private void toolStripStatusLabel3_Click(object sender, EventArgs e)
        {
            if (_geoPoints.Count >= 2)
            {
                double perimeter = GeometryHelper.CalculatePerimeter(_geoPoints);
                MessageBox.Show($"总长度: {perimeter:F2} 千米");
            }
            else
            {
                MessageBox.Show("至少需要2个坐标点才能计算周长。");
            }
        }

        private void toolStripStatusLabel4_Click(object sender, EventArgs e)
        {
            _clickDetector?.StartDetectingClick();
        }

        private void toolStripStatusLabel5_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*";
                openFileDialog.Title = "选择一个地理坐标CSV文件";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFilePath = openFileDialog.FileName;

                    _geoPoints = GeoDataProcessor.LoadGeoPointsFromFile(selectedFilePath);
                    _mapPainter.SetData(_geoPoints);

                    RectangleF screenArea = panel1.ClientRectangle;
                    List<PointF> screenPoints = GeoDataProcessor.ConvertToScreen(_geoPoints, screenArea);
                    _clickDetector = new ClickDetector(panel1, _geoPoints, screenPoints);

                    MessageBox.Show("地理数据加载成功！");
                }
            }
        }

        internal static class GeoDataProcessor
        {
            private const double EarthRadius = 6371000;
            private const double MetersPerDegree = EarthRadius * Math.PI / 180;

            public static List<GeoPoint> LoadGeoPointsFromFile(string filePath)
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"文件未找到：{filePath}");
                    return new List<GeoPoint>();
                }

                CommFunc commFunc = new CommFunc();
                return commFunc.read_stringLines_from_TxtFile(filePath)
                               .Select(ParseGeoPoint)
                               .Where(point => point != null)
                               .ToList();
            }

            private static GeoPoint ParseGeoPoint(string line)
            {
                string[] parts = line.Split(',');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out double latitude) &&
                    double.TryParse(parts[1], out double longitude))
                {
                    return new GeoPoint(latitude, longitude);
                }
                Console.WriteLine($"跳过格式错误的行: {line}");
                return null;
            }

            public static List<PointF> ConvertToScreen(List<GeoPoint> geoPoints, RectangleF screenArea)
            {
                if (geoPoints == null || !geoPoints.Any()) return new List<PointF>();

                double minLon = geoPoints.Min(p => p.Longitude);
                double maxLon = geoPoints.Max(p => p.Longitude);
                double minLat = geoPoints.Min(p => p.Latitude);
                double maxLat = geoPoints.Max(p => p.Latitude);

                float deltaLon = (float)(maxLon - minLon);
                float deltaLat = (float)(maxLat - minLat);

                if (deltaLon == 0 || deltaLat == 0)
                {
                    return geoPoints.Select(_ => new PointF(screenArea.Width / 2, screenArea.Height / 2)).ToList();
                }

                float scaleX = screenArea.Width / deltaLon;
                float scaleY = screenArea.Height / deltaLat;

                return geoPoints.Select(p => new PointF(
                    (float)((p.Longitude - minLon) * scaleX),
                    screenArea.Height - (float)((p.Latitude - minLat) * scaleY)
                )).ToList();
            }

            public static double CalculateArea(List<GeoPoint> geoPoints)
            {
                if (geoPoints == null || geoPoints.Count < 3) return 0;

                var reference = geoPoints[0];
                var localPoints = ConvertToLocal(geoPoints, reference);
                return ApplyShoelaceFormula(localPoints);
            }

            private static List<GeoPoint> ConvertToLocal(List<GeoPoint> points, GeoPoint reference)
            {
                double refLatRad = reference.Latitude * Math.PI / 180;
                double cosRefLat = Math.Cos(refLatRad);

                return points.Select(point =>
                {
                    double deltaLon = point.Longitude - reference.Longitude;
                    double deltaLat = point.Latitude - reference.Latitude;
                    double east = deltaLon * MetersPerDegree * cosRefLat;
                    double north = deltaLat * MetersPerDegree;
                    return new GeoPoint(north, east);
                }).ToList();
            }

            private static double ApplyShoelaceFormula(List<GeoPoint> points)
            {
                double sum = 0;
                int n = points.Count;
                for (int i = 0; i < n; i++)
                {
                    var current = points[i];
                    var next = points[(i + 1) % n];
                    sum += (current.Longitude * next.Latitude - next.Longitude * current.Latitude);
                }
                return Math.Abs(sum / 2.0) / 1000000.0;
            }
        }

        internal class MapPainter
        {
            private readonly Panel _targetPanel;
            private List<GeoPoint> _pointsToDraw = new List<GeoPoint>();
            private bool _shouldPaint = false;
            private Timer _fadeInTimer = new Timer { Interval = 200 };
            private int _currentAlpha = 0;
            private const int FadeSteps = 25;

            public MapPainter(Panel panel)
            {
                _targetPanel = panel;
                _targetPanel.Paint += OnPaint;
                _targetPanel.Resize += OnResize;
                _fadeInTimer.Tick += OnFadeInTick;
            }

            public void SetData(List<GeoPoint> points)
            {
                _pointsToDraw = points;
            }

            public void StartPainting()
            {
                if (_pointsToDraw.Count >= 2)
                {
                    _shouldPaint = true;
                    _currentAlpha = 0;
                    _fadeInTimer.Start();
                    _targetPanel.Invalidate();
                }
                else
                {
                    MessageBox.Show("坐标点少于2个，无法开始绘制。");
                }
            }

            private void OnPaint(object sender, PaintEventArgs e)
            {
                if (_shouldPaint && _pointsToDraw.Count >= 2)
                {
                    using (Graphics g = e.Graphics)
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        RectangleF screenArea = _targetPanel.ClientRectangle;
                        List<PointF> screenPoints = GeoDataProcessor.ConvertToScreen(_pointsToDraw, screenArea);

                        if (screenPoints.Count >= 2)
                        {
                            using (Pen pen = new Pen(Color.FromArgb(_currentAlpha, Color.Black), 2))
                            {
                                g.DrawLines(pen, screenPoints.ToArray());
                            }
                        }
                    }
                }
            }

            private void OnResize(object sender, EventArgs e)
            {
                _targetPanel.Invalidate();
            }

            private void OnFadeInTick(object sender, EventArgs e)
            {
                _currentAlpha += (255 / FadeSteps);
                if (_currentAlpha >= 255)
                {
                    _currentAlpha = 255;
                    _fadeInTimer.Stop();
                }
                _targetPanel.Invalidate();
            }
        }
    }
}
