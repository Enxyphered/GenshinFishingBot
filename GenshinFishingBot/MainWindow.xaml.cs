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

namespace GenshinFishingBot
{
    public partial class MainWindow : Window
    {
        private static string PATH = "pos.json";

        ValuesViewModel _values;
        Direct3DCapture _cap;
        InputSimulator _is;

        public MainWindow()
        {
            if (File.Exists(PATH))
                _values = JsonConvert.DeserializeObject<ValuesViewModel>(File.ReadAllText(PATH));
            else
                _values = new ValuesViewModel() { X = 715, Y = 97, Width = 490, Height = 29 };
            _values.PropertyChanged += SaveChanges;
            DataContext = _values;
            _cap = new Direct3DCapture();
            _cap.CapturedEvent += Captured;
            _is = new InputSimulator();
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
                    ).CvtColor(ColorConversionCodes.GRAY2RGBA)
                     .MedianBlur(3)
                );

            var midPoint = _values.Height / 2;
            var width = screenShot.Width;
            int arrowStart = 0;
            int arrowEnd = 0;
            int cursor = 0;
            int detections = 0;
            bool whiteBlock = false;
            for (int i = 0; i < width; i++)
            {
                if (screenShotProcessed.Get<Vec4b>(midPoint, i).Item0 > 50)
                {
                    if (whiteBlock)
                        continue;

                    whiteBlock = true;
                    detections++;
                    if (screenShotProcessed.Get<Vec4b>(_values.Height - 1, i).Item0 > 50)
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

            screenShot.DrawMarker(new Point(arrowStart, midPoint), new Scalar(255, 255, 0, 255));
            screenShot.DrawMarker(new Point(arrowEnd, midPoint), new Scalar(255, 255, 0, 255));
            screenShot.DrawMarker(new Point(cursor, midPoint), new Scalar(255, 0, 0, 255));
            screenShotProcessed.DrawMarker(new Point(arrowStart, midPoint), new Scalar(255, 255, 0, 255));
            screenShotProcessed.DrawMarker(new Point(arrowEnd, midPoint), new Scalar(255, 255, 0, 255));
            screenShotProcessed.DrawMarker(new Point(cursor, midPoint), new Scalar(255, 0, 0, 255));

            try
            {
                Dispatcher.Invoke(() => { img.Source = screenShot.ToWriteableBitmap(); img2.Source = screenShotProcessed.ToWriteableBitmap(); });
            }
            catch (Exception) { }

            if (cursor < (arrowStart + arrowEnd) / 2 &&
                cursor > 0 &&
                arrowEnd > 0)
                _is.Mouse.LeftButtonClick();
        }

        private void Win_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(DetectionLoop);
        }

        private void DetectionLoop()
        {
            while (true)
            {
                _cap.NextFrame();
            }
        }

    }
}
