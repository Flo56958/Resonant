using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Resonant.Player {
    class MusicController {

        private static MusicController _controller;

        public static MusicController GetMusicController() {
            return _controller ?? (_controller = new MusicController());
        }


        private readonly Queue<StorageFile> _musicQueue = new Queue<StorageFile>();
        private readonly MediaPlayer _mediaPlayer = new MediaPlayer()
        {
            AutoPlay = true
        };
        private MusicProperties _currentMusic;
        private StorageFile _currentFile;

        private MusicController() {
            _mediaPlayer.MediaEnded += MediaPlayerOnMediaEnded;
            _mediaPlayer.MediaFailed += MediaPlayerOnMediaFailed;
            _mediaPlayer.SourceChanged += MediaPlayerOnSourceChanged;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000 / 60) };
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void MediaPlayerOnSourceChanged(MediaPlayer sender, object args) {
            if (_currentMusic != null) {
                MainPage.Model.CurrentlyPlaying = _currentMusic.Album + " - " + _currentMusic.Title;
            }

            DispatcherHelper.ExecuteOnUIThreadAsync(() =>
            {
                MainPage.Window.CurrentlyPlayingListView.Items?.Clear();
                if (_currentFile != null) {
                    var item = CreateListViewItem(_currentFile);
                    item.IsEnabled = true;
                    MainPage.Window.CurrentlyPlayingListView.Items?.Add(item);
                }

                foreach (var file in _musicQueue) {
                    MainPage.Window.CurrentlyPlayingListView.Items?.Add(CreateListViewItem(file));
                }
            });
        }

        private static ListViewItem CreateListViewItem(StorageFile file) {
            var properties = file.Properties.GetMusicPropertiesAsync().GetAwaiter().GetResult();
            var upperGrid = new Grid() {
                ColumnDefinitions = { new ColumnDefinition() { Width = GridLength.Auto }, new ColumnDefinition() }
            };
            var grid = new Grid() {
                RowDefinitions = { new RowDefinition(), new RowDefinition() }
            };
            var grid2 = new Grid() {
                ColumnDefinitions = { new ColumnDefinition() { Width = GridLength.Auto }, new ColumnDefinition(), new ColumnDefinition() }
            };
            Grid.SetRow(grid2, 0);
            grid.Children.Add(grid2);
            if (properties.Title != null) {
                var title = new ContentControl() {
                    Content = properties.Title,
                    FontWeight = FontWeights.Bold,
                    FontSize = 20,
                    Opacity = 1,
                    Margin = new Thickness(3),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                Grid.SetColumn(title, 0);
                grid2.Children.Add(title);
            }
            if (properties.Artist != null) {
                var artist = new ContentControl() {
                    Content = properties.Artist,
                    Margin = new Thickness(3),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                Grid.SetColumn(artist, 1);
                grid2.Children.Add(artist);
            }
            if (properties.Album != null) {
                var album = new ContentControl() {
                    Content = properties.Album,
                    Opacity = 0.5,
                    Margin = new Thickness(3),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                Grid.SetColumn(album, 2);
                grid2.Children.Add(album);
            }
            var grid3 = new Grid() {
                ColumnDefinitions =
                {
                    new ColumnDefinition() { Width = GridLength.Auto },
                    new ColumnDefinition() { Width = GridLength.Auto },
                    new ColumnDefinition() { Width = GridLength.Auto },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star)}
                }
            };
            if (file.Path != null) {
                var path = new ContentControl()
                {
                    Content = file.Path,
                    FontSize = 10,
                    Opacity = 0.5,
                    Margin = new Thickness(3),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(path, 0);
                grid3.Children.Add(path);
            }
            var bitrate = new ContentControl() {
                    Content = properties.Bitrate + " bps",
                    FontSize = 10,
                    Opacity = 0.5,
                    Margin = new Thickness(3),
                    VerticalAlignment = VerticalAlignment.Top
                };
            Grid.SetColumn(bitrate, 1);
            grid3.Children.Add(bitrate);

            var duration = new ContentControl() {
                Content = properties.Duration.ToString().Split('.')[0],
                FontSize = 10,
                Opacity = 0.5,
                Margin = new Thickness(3),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(duration, 2);
            grid3.Children.Add(duration);

            Grid.SetRow(grid3, 1);
            grid.Children.Add(grid3);

            using var thumbnail = file.GetThumbnailAsync(ThumbnailMode.ListView).GetAwaiter().GetResult();
            if (thumbnail != null) {
                var bitmapImage = new BitmapImage();
                bitmapImage.SetSource(thumbnail);
                var image = new Image() {
                    Source = bitmapImage,
                };
            
                Grid.SetColumn(image, 0);
                upperGrid.Children.Add(image);
            }

            Grid.SetColumn(grid, 1);
            upperGrid.Children.Add(grid);

            return new ListViewItem() {
                Content = upperGrid,
                IsEnabled = false
            };
        }

        private void MediaPlayerOnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args) {
            Debug.WriteLine("Media Failed!");
            Debug.WriteLine(args.Error);
            Debug.WriteLine(args.ErrorMessage);
        }

        private void timer_Tick(object sender, object e) {
            if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing) {
                MainPage.Model.CurrentSeconds = _mediaPlayer.PlaybackSession.Position.TotalSeconds;
                MainPage.Model.MusicLengthSeconds = _mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds;
            }
            else if (_mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Paused) {
                MainPage.Model.CurrentSeconds = 0;
            }
        }

        private void MediaPlayerOnMediaEnded(MediaPlayer sender, object args) {
            NextTrack();
        }

        public void TogglePlayPause() {
            switch (_mediaPlayer.PlaybackSession.PlaybackState) {
                case MediaPlaybackState.Playing:
                    _mediaPlayer.Pause();
                    break;
                case MediaPlaybackState.Paused:
                    _mediaPlayer.Play();
                    break;
                case MediaPlaybackState.None:
                case MediaPlaybackState.Opening:
                case MediaPlaybackState.Buffering:
                default:
                    if (!_musicQueue.TryDequeue(out var file)) return;
                    _currentFile = file;
                    _currentMusic = file.Properties.GetMusicPropertiesAsync().GetAwaiter().GetResult();
                    _mediaPlayer.SetFileSource(file);
                    break;
            }
        }

        public void SetAudioDevice(DeviceInformation selectedDevice) {
            _mediaPlayer.AudioDevice = selectedDevice;
        }

        public void PlayMusic(StorageFile file) {
            _musicQueue.Clear();
            _currentMusic = file.Properties.GetMusicPropertiesAsync().GetAwaiter().GetResult();
            _currentFile = file;
            _mediaPlayer.SetFileSource(file);
        }

        public void AddMusic(IReadOnlyList<StorageFile> files) {
            foreach (var file in files) {
                _musicQueue.Enqueue(file);
            }
            MediaPlayerOnSourceChanged(null, null);
        }

        public void SetSpeed(double value) {
            _mediaPlayer.PlaybackSession.PlaybackRate = value;
        }

        public void SetVolume(double value) {
            _mediaPlayer.Volume = value;
        }

        public void SetAudioBalance(double value) {
            _mediaPlayer.AudioBalance = value;
        }

        public void NextTrack() {
            if (_musicQueue.TryDequeue(out var file)) {
                _currentFile = file;
                _currentMusic = file.Properties.GetMusicPropertiesAsync().GetAwaiter().GetResult();
                _mediaPlayer.SetFileSource(file);
                _mediaPlayer.Play();
            } else {
                _currentFile = null;
                _currentMusic = null;
                _mediaPlayer.Source = null;
                MainPage.Model.CurrentSeconds = 0;
                MainPage.Model.CurrentlyPlaying = "Nothing is playing...";
            }
        }

        public void PreviousTrack() {

        }
    }
}
