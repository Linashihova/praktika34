using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DragAndDrop_Shihova
{
    public partial class MainWindow : Window
    {
        public DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private Size savedSize;

        public MainWindow()
        {
            InitializeComponent();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new System.TimeSpan(0, 0, 0, 0, 1000 / 60);
        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            image.Margin = new Thickness(Mouse.GetPosition(this).X - 25, Mouse.GetPosition(this).Y - 25, 0, 0);
        }
        private void image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (frame.Visibility == Visibility.Visible) return;
            dispatcherTimer.Start();
        }

        private void image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dispatcherTimer.Stop();
        }

        private void OpenCropPage_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            savedSize = new Size(Width, Height);
            Width = 980;
            Height = 760;
            frame.Visibility = Visibility.Visible;
            frame.Navigate(new CropPage());
        }

        public void CloseCropPage()
        {
            frame.Content = null;
            frame.Visibility = Visibility.Collapsed;
            Width = savedSize.Width;
            Height = savedSize.Height;
        }
    }
}
