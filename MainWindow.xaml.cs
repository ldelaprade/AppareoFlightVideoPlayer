using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AppareoFlightVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private DispatcherTimer _timer;
        private bool _isDraggingSeekBar = false;
        private bool _isFullscreen = false;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private ResizeMode _previousResizeMode;

        // The error CS1061 indicates that 'MediaPlayer' does not contain a definition for 'SetVideoBackground'.
        // Based on the provided type signatures, there is no method named 'SetVideoBackground' in the 'MediaPlayer' class.
        // To fix this issue, the line `_mediaPlayer.SetVideoBackground(0x000000);` should be removed as it is invalid.

        public MainWindow()
        {
            InitializeComponent();

            // Preferred path to the VLC native libraries
            var preferredLibVlcPath = Path.Combine(AppContext.BaseDirectory, "libvlc", "win-x64");

            // Fallback path (base directory)
            var fallbackLibVlcPath = AppContext.BaseDirectory;

            // Determine which path to use
            string libVlcPathToUse = Directory.Exists(preferredLibVlcPath) && File.Exists(Path.Combine(preferredLibVlcPath, "libvlc.dll")) ? 
                preferredLibVlcPath
                :
                fallbackLibVlcPath;

            // Initialize LibVLC with the selected path
            Core.Initialize(libVlcPathToUse);

            //_libVLC = new LibVLC("--video-background=0x000000"); // Black background
            //_libVLC = new LibVLC("--no - audio");
            _libVLC = new LibVLC();

            _mediaPlayer = new MediaPlayer(_libVLC);
            //_mediaPlayer.Scale = 1; // 1 usually means "fill"

            videoView.MediaPlayer = _mediaPlayer;

            _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
            _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;

            VolumeSlider.Value = _mediaPlayer.Volume;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.ts|All Files|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                _mediaPlayer.Stop();
                using var media = new Media(_libVLC, dialog.FileName, FromType.FromPath);
                _mediaPlayer.Play(media);
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer.Media != null)
                _mediaPlayer.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer.CanPause)
                _mediaPlayer.Pause();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Stop();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null)
                _mediaPlayer.Volume = (int)VolumeSlider.Value;
        }

        private void MediaPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SeekBar.Maximum = e.Length;
            });
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!_isDraggingSeekBar)
                {
                    SeekBar.Value = e.Time;
                }
                UpdateTimeText();
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer.Media != null && !_isDraggingSeekBar)
            {
                SeekBar.Value = _mediaPlayer.Time;
                UpdateTimeText();
            }
        }

        private void SeekBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSeekBar = true;
        }

        private void SeekBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_mediaPlayer.Media != null)
            {
                _mediaPlayer.Time = (long)SeekBar.Value;
            }
            _isDraggingSeekBar = false;
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isFullscreen)
            {
                _previousWindowState = WindowState;
                _previousWindowStyle = WindowStyle;
                _previousResizeMode = ResizeMode;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                _isFullscreen = true;
                FullscreenButton.Content = "Exit fullscreen";

            }
            else
            {
                WindowStyle = _previousWindowStyle;
                WindowState = _previousWindowState;
                ResizeMode = _previousResizeMode;
                _isFullscreen = false;
                FullscreenButton.Content = "Fullscreen";


            }
            // Force redraw
            this.InvalidateVisual();
            this.UpdateLayout();
        }

        private void UpdateTimeText()
        {
            var current = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            var total = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
            TimeText.Text = $"{current:hh\\:mm\\:ss} / {total:hh\\:mm\\:ss}";
        }
    }
}
