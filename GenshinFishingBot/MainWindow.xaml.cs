using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Newtonsoft.Json;
using WindowsInput;
using Window = System.Windows.Window;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using System.Diagnostics;

namespace GenshinFishingBot
{
    public partial class MainWindow : Window
    {
        private static string PATH = "pos.json";

        private bool _isDown;
        public ValuesViewModel _values;
        private Direct3DCapture _cap;
        private InputSimulator _is;
        private Stopwatch _sw;
        private int currentCount = 0;
        private long currentSum = 0;
        private int sampleCount = 30;

        public MainWindow()
        {
            _cap = new Direct3DCapture();
            _is = new InputSimulator();
            _sw = new Stopwatch();

            if (File.Exists(PATH))
                _values = JsonConvert.DeserializeObject<ValuesViewModel>(File.ReadAllText(PATH));
            else
                _values = new ValuesViewModel() { X = 715, Y = 96, Width = 490, Height = 29 };

            _values.PropertyChanged += SaveChanges;

            DataContext = _values;
            InitializeComponent();
        }

        private void SaveChanges(object sender, PropertyChangedEventArgs e)
        {
            File.WriteAllText(PATH, JsonConvert.SerializeObject(_values));
        }

        private void Captured(Span<byte> buffer)
        {
            var screenShot = new Mat(_cap.Height, _cap.Width, MatType.CV_8UC4, buffer.ToArray());
            screenShot = new Mat(screenShot, new Rect(_values.X, _values.Y, _values.Width, _values.Height));
            Mat screenShotProcessed = screenShot.BitwiseAnd(
                screenShot.InRange(
                    new Scalar(180, 240, 240, 255),
                    new Scalar(210, 255, 255, 255)
                    ).CvtColor(ColorConversionCodes.GRAY2RGBA).MedianBlur(1)
                );
            screenShotProcessed = screenShotProcessed.Blur(new OpenCvSharp.Size(2, 2));

            var yMidPoint = _values.Height / 2;
            var width = screenShot.Width;
            int arrowStart = 0;
            int arrowEnd = 0;
            int cursor = 0;
            int detections = 0;
            bool whiteBlock = false;
            for (int i = 0; i < width; i++)
            {
                var pixel = screenShotProcessed.Get<Vec4b>(yMidPoint, i);
                if (pixel.Item0 >= 50 && pixel.Item1 >= 50 && pixel.Item2 >= 50)
                {
                    if (whiteBlock)
                        continue;

                    whiteBlock = true;
                    detections++;
                    pixel = screenShotProcessed.Get<Vec4b>(_values.Height - 1, i);
                    if (pixel.Item0 >= 50 && pixel.Item1 >= 50 && pixel.Item2 >= 50)
                        cursor = i;
                    else if (arrowStart == 0)
                        arrowStart = i;
                    else
                        arrowEnd = i;
                    if (detections >= 3)
                        break;
                }
                else
                    whiteBlock = false;
            }

            var mid = (arrowStart + arrowEnd) / 2;
            if (cursor < mid)
            {
                if (cursor > 0 && arrowEnd > 0 && !_isDown)
                {
                    _is.Mouse.LeftButtonDown();
                    _isDown = true;
                }
            }
            else if (_isDown && arrowEnd > 0)
            {
                _is.Mouse.LeftButtonUp();
                _isDown = false;
            }

            if (arrowStart > 0)
            {
                screenShot.DrawMarker(new Point(arrowStart, yMidPoint), new Scalar(255, 0, 0, 255), MarkerTypes.Diamond, 5, 3);
                screenShot.DrawMarker(new Point(arrowEnd, yMidPoint), new Scalar(255, 0, 0, 255), MarkerTypes.Diamond, 5, 3);
                screenShot.DrawMarker(new Point(cursor, yMidPoint), new Scalar(0, 0, 255, 255), MarkerTypes.Diamond, 5, 3);
                screenShotProcessed.Line(cursor, yMidPoint, mid, yMidPoint, new Scalar(0, 0, 100, 255));
                screenShotProcessed.PutText($"{ Math.Abs(mid - cursor) }px",
                    new Point(cursor + 5, yMidPoint + 5),
                    HersheyFonts.HersheyComplexSmall, 0.6,
                    new Scalar(255, 255, 255, 255));
            }

            try
            {
                Dispatcher.Invoke(() => { img.Source = screenShot.ToWriteableBitmap(); img2.Source = screenShotProcessed.ToWriteableBitmap(); });
            }
            catch (Exception) { }
        }

        private void Win_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(DetectionLoop);
        }

        private void DetectionLoop()
        {
            while (true)
            {
                _sw.Restart();
                var frame = _cap.NextFrame();
                Captured(frame);
                _sw.Stop();
                currentSum += _sw.ElapsedMilliseconds;
                if (++currentCount >= sampleCount)
                {
                    _values.PerfText = $"{ currentSum / sampleCount }ms";
                    currentSum = 0;
                    currentCount = 0;
                }
            }
        }

    }
}
