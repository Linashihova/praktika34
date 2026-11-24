using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DragAndDrop_Shihova
{
    public partial class CropPage : Page
    {
        private const double MinFrameSize = 20;
        private const double HandleSize = 8;

        public DispatcherTimer dispatcherTimer = new DispatcherTimer();

        private BitmapSource originalImage;
        private BitmapSource currentImage;
        private double scale;

        private Rect imageRect;
        private Rect frameRect;

        private string dragMode;
        private Point dragStartMouse;
        private Rect dragStartFrame;

        private readonly Dictionary<string, Rectangle> handles = new Dictionary<string, Rectangle>();

        public CropPage()
        {
            InitializeComponent();

            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / 60);

            CreateHandles();

            var image = new BitmapImage(new Uri("pack://application:,,,/1.png"));
            image.Freeze();
            originalImage = image;
            ShowImage(originalImage);
        }

        private void CreateHandles()
        {
            string[] modes = { "NW", "N", "NE", "E", "SE", "S", "SW", "W" };
            Cursor[] cursors =
            {
                Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE,
                Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE
            };

            for (int i = 0; i < modes.Length; i++)
            {
                var handle = new Rectangle
                {
                    Width = HandleSize,
                    Height = HandleSize,
                    Fill = new SolidColorBrush(Color.FromRgb(0x3A, 0x8E, 0xE6)),
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    Cursor = cursors[i],
                    Tag = modes[i]
                };
                handle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
                handles[modes[i]] = handle;
                workArea.Children.Add(handle);
            }
        }

        private void Frame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag("Move", Cursors.SizeAll, e);
        }

        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var handle = (Rectangle)sender;
            StartDrag((string)handle.Tag, handle.Cursor, e);
        }

        private void StartDrag(string mode, Cursor cursor, MouseButtonEventArgs e)
        {
            dragMode = mode;
            dragStartMouse = e.GetPosition(workArea);
            dragStartFrame = frameRect;

            workArea.Cursor = cursor;
            workArea.CaptureMouse();

            dispatcherTimer.Start();
            e.Handled = true;
        }

        private void StopDrag()
        {
            dispatcherTimer.Stop();
            dragMode = null;
            workArea.Cursor = null;
            workArea.ReleaseMouseCapture();
        }

        private void Page_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dragMode != null) StopDrag();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                StopDrag();
                return;
            }

            Point mouse = Mouse.GetPosition(workArea);
            double dx = mouse.X - dragStartMouse.X;
            double dy = mouse.Y - dragStartMouse.Y;

            double left = dragStartFrame.Left;
            double top = dragStartFrame.Top;
            double right = dragStartFrame.Right;
            double bottom = dragStartFrame.Bottom;

            if (dragMode == "Move")
            {
                double w = dragStartFrame.Width, h = dragStartFrame.Height;
                left = Clamp(left + dx, imageRect.Left, imageRect.Right - w);
                top = Clamp(top + dy, imageRect.Top, imageRect.Bottom - h);
                right = left + w;
                bottom = top + h;
            }
            else
            {
                if (dragMode.Contains("W")) left = Clamp(left + dx, imageRect.Left, right - MinFrameSize);
                if (dragMode.Contains("E")) right = Clamp(right + dx, left + MinFrameSize, imageRect.Right);
                if (dragMode.Contains("N")) top = Clamp(top + dy, imageRect.Top, bottom - MinFrameSize);
                if (dragMode.Contains("S")) bottom = Clamp(bottom + dy, top + MinFrameSize, imageRect.Bottom);
            }

            frameRect = new Rect(new Point(left, top), new Point(right, bottom));
            UpdateFrame();
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private void ShowImage(BitmapSource image)
        {
            currentImage = image;

            double areaW = workArea.Width, areaH = workArea.Height;
            scale = Math.Min(1.0, Math.Min(areaW / image.PixelWidth, areaH / image.PixelHeight));

            double w = image.PixelWidth * scale;
            double h = image.PixelHeight * scale;
            imageRect = new Rect((areaW - w) / 2, (areaH - h) / 2, w, h);

            photo.Source = image;
            photo.Width = w;
            photo.Height = h;
            Canvas.SetLeft(photo, imageRect.Left);
            Canvas.SetTop(photo, imageRect.Top);

            frameRect = imageRect;
            if (w > MinFrameSize * 2 && h > MinFrameSize * 2)
                frameRect.Inflate(-w * 0.1, -h * 0.1);

            realSizeText.Text = image.PixelWidth + " x " + image.PixelHeight;
            scaledSizeText.Text = Math.Round(w) + " x " + Math.Round(h);

            UpdateFrame();
        }

        private void UpdateFrame()
        {
            Canvas.SetLeft(frame, frameRect.Left);
            Canvas.SetTop(frame, frameRect.Top);
            frame.Width = frameRect.Width;
            frame.Height = frameRect.Height;

            double cx = frameRect.Left + frameRect.Width / 2;
            double cy = frameRect.Top + frameRect.Height / 2;
            PlaceHandle("NW", frameRect.Left, frameRect.Top);
            PlaceHandle("N", cx, frameRect.Top);
            PlaceHandle("NE", frameRect.Right, frameRect.Top);
            PlaceHandle("E", frameRect.Right, cy);
            PlaceHandle("SE", frameRect.Right, frameRect.Bottom);
            PlaceHandle("S", cx, frameRect.Bottom);
            PlaceHandle("SW", frameRect.Left, frameRect.Bottom);
            PlaceHandle("W", frameRect.Left, cy);

            shade.Data = new CombinedGeometry(GeometryCombineMode.Exclude,
                new RectangleGeometry(imageRect), new RectangleGeometry(frameRect));

            Int32Rect crop = GetCropRect();
            outputSizeText.Text = crop.Width + " x " + crop.Height;
        }

        private void PlaceHandle(string mode, double x, double y)
        {
            Canvas.SetLeft(handles[mode], x - HandleSize / 2);
            Canvas.SetTop(handles[mode], y - HandleSize / 2);
        }

        private Int32Rect GetCropRect()
        {
            int x = (int)Math.Round((frameRect.Left - imageRect.Left) / scale);
            int y = (int)Math.Round((frameRect.Top - imageRect.Top) / scale);
            int w = (int)Math.Round(frameRect.Width / scale);
            int h = (int)Math.Round(frameRect.Height / scale);

            x = Math.Max(0, Math.Min(x, currentImage.PixelWidth - 1));
            y = Math.Max(0, Math.Min(y, currentImage.PixelHeight - 1));
            w = Math.Max(1, Math.Min(w, currentImage.PixelWidth - x));
            h = Math.Max(1, Math.Min(h, currentImage.PixelHeight - y));
            return new Int32Rect(x, y, w, h);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Window.GetWindow(this)).CloseCropPage();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ShowImage(originalImage);
        }

        private void Crop_Click(object sender, RoutedEventArgs e)
        {
            var cropped = new CroppedBitmap(currentImage, GetCropRect());
            cropped.Freeze();
            ShowImage(cropped);
        }
    }
}
