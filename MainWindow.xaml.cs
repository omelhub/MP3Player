using Microsoft.VisualBasic.Logging;
using NAudio.Wave;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TagLib;

namespace MP3Player
{
    public class ResourceWrapper
    {
        public static string MenuText => Resources.Menu_Text;
        public static string OpenText => Resources.Open_Text;

    }
    public partial class MainWindow : Window
    {
        private WaveOutEvent _waveOut;
        private AudioFileReader _audioFileReader;
        private bool _waveIsRunning;
        BitmapImage logo = new BitmapImage(new Uri("pack://application:,,,/MP3Player;component/logo.png"));

        public MainWindow()
        {
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            InitializeComponent();
        }
        private void UpdateCurrentTime(object sender, EventArgs e)
        {
            if (_audioFileReader != null && _waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing)
            {
                currentDurationLabel.Text = _audioFileReader.CurrentTime.ToString(@"mm\:ss");
                PositionSlider.Value = _audioFileReader.CurrentTime.TotalSeconds;
                PositionSlider.SelectionEnd = PositionSlider.Value;
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "MP3 Files (*.mp3)|*.mp3",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == true)
            {
                if (_waveOut != null)
                {
                    _waveOut.Stop();
                    _audioFileReader.Dispose();
                    _waveOut.Dispose();
                    Icon.Source = logo;
                }
                _audioFileReader = new AudioFileReader(openFileDialog.FileName);
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioFileReader);
                _waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
                _waveOut.Volume = (float)VolumeSlider.Value / 10;
                PositionSlider.Value = 0;
                PositionSlider.Maximum = (int)_audioFileReader.TotalTime.TotalSeconds;

                PositionSlider.MouseDown += PositionSlider_MouseDown;

                SongNameTextBlock.Text = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.SafeFileName);
                currentDurationLabel.Text = _audioFileReader.CurrentTime.ToString(@"mm\:ss");
                totalDurationLabel.Text = _audioFileReader.TotalTime.ToString(@"mm\:ss");

                using (var file = TagLib.File.Create(openFileDialog.FileName))
                {
                    var tag = file.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Id3v2.Tag;
                    if (tag != null)
                    {
                        var cover = tag.Pictures.FirstOrDefault();
                        if (cover != null)
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = new MemoryStream(cover.Data.Data);
                            bitmap.EndInit();

                            Icon.Source = bitmap;
                        }
                    }
                }
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (_waveOut != null && _audioFileReader != null)
            {
                PlayButton.Visibility = Visibility.Collapsed;
                PauseButton.Visibility = Visibility.Visible;

                _audioFileReader.CurrentTime = TimeSpan.FromSeconds(PositionSlider.Value);
                _waveOut.Play();

                DispatcherTimer timer = new();
                //timer.Interval = TimeSpan.FromMilliseconds(100);
                timer.Tick += UpdateCurrentTime;
                timer.Start();
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (_waveOut != null)
            {
                PauseButton.Visibility = Visibility.Collapsed;
                PlayButton.Visibility = Visibility.Visible;
                _waveOut.Pause();
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (_waveOut != null)
            {
                PauseButton.Visibility = Visibility.Collapsed;
                PlayButton.Visibility = Visibility.Visible;
                _waveOut.Stop();
                _audioFileReader.Dispose();
                _audioFileReader = null;
                _waveOut.Dispose();
                _waveOut = null;
                SongNameTextBlock.Text = "";
                currentDurationLabel.Text = "00:00";
                totalDurationLabel.Text = "00:00";
                Icon.Source = new BitmapImage(new Uri("pack://application:,,,/MP3Player;component/logo.png"));
                PositionSlider.SelectionEnd = 0;
            }
        }

        private void PositionSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (_waveOut != null)
            {
                _waveIsRunning = (_waveOut.PlaybackState == PlaybackState.Playing);
                _waveOut.Pause();
            }
        }

        private void PositionSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            
            if (_waveOut != null && _audioFileReader != null)
            {
                _audioFileReader.CurrentTime = TimeSpan.FromSeconds(PositionSlider.Value);
                currentDurationLabel.Text = _audioFileReader.CurrentTime.ToString(@"mm\:ss");
                PositionSlider.SelectionEnd = PositionSlider.Value;
                if (_waveIsRunning)
                {
                    _waveOut.Play();
                }              
            }
        }

        private void PositionSlider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_audioFileReader != null && _waveOut != null)
            {
                _audioFileReader.CurrentTime = TimeSpan.FromSeconds(PositionSlider.Value);
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VolumeSlider.SelectionEnd = VolumeSlider.Value;
            if (_waveOut != null)
            {
                _waveOut.Volume = (float)VolumeSlider.Value / 10;
            }
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            PositionSlider.Value = 0;
            PositionSlider.SelectionEnd = PositionSlider.Value;
            currentDurationLabel.Text = "00:00";

            if (_audioFileReader != null)
            {
                _audioFileReader.Position = 0;
                _waveOut.Init(_audioFileReader);
                currentDurationLabel.Text = _audioFileReader.CurrentTime.ToString(@"mm\:ss");
                
                if (RepeatButton.IsChecked.GetValueOrDefault())
                {
                    _waveOut.Play();

                    DispatcherTimer timer = new();
                    timer.Tick += UpdateCurrentTime;
                    timer.Start();
                }
                else
                {
                    PlayButton.Visibility = Visibility.Visible;
                    PauseButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}