using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Resonant.Player {
    class MusicController {

        private static MusicController _controller;

        public static MusicController GetMusicController() {
            return _controller ??= new MusicController();
        }

        private readonly Playlist _playerPlaylist = new Playlist();
        private readonly MediaPlayer _mediaPlayer = new MediaPlayer() {
            AutoPlay = true
        };

        private MusicController() {
            _mediaPlayer.MediaEnded += MediaPlayerOnMediaEnded;
            _mediaPlayer.MediaFailed += MediaPlayerOnMediaFailed;
            _mediaPlayer.SourceChanged += MediaPlayerOnSourceChanged;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000 / 60) };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void MediaPlayerOnSourceChanged(MediaPlayer sender, object args) {
            var current = _playerPlaylist.GetCurrentIndex();
            if (current != -1) {
                var musicFile = _playerPlaylist.Music[current];
                MainPage.Model.CurrentlyPlaying = musicFile?.Album + " - " + musicFile?.Title;
            }
        }


        private static void MediaPlayerOnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args) {
            Debug.WriteLine("Media Failed!");
            Debug.WriteLine(args.Error);
            Debug.WriteLine(args.ErrorMessage);
        }

        private void Timer_Tick(object sender, object e) {
            if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing) {
                MainPage.Model.CurrentSeconds = _mediaPlayer.PlaybackSession.Position.TotalSeconds;
                MainPage.Model.MusicLengthSeconds = _mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds;
            } else if (_mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Paused) {
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
                    var idx = _playerPlaylist.GetCurrentIndex();
                    if (idx == -1) return;
                    if (_mediaPlayer.Source is MediaSource source) {
                        source.Dispose();
                    }

                    var mf = _playerPlaylist.Music[idx];
                    mf.CurrentlyPlaying = true;
                    _mediaPlayer.Source = MediaSource.CreateFromStorageFile(mf.StorageFile);
                    break;
            }
        }

        public void SetAudioDevice(DeviceInformation selectedDevice) {
            _mediaPlayer.AudioDevice = selectedDevice;
        }

        public void AddMusic(IReadOnlyList<StorageFile> files) {
            foreach (var storageFile in files) {
                    _playerPlaylist.AddMusicFile(new MusicFile(storageFile));
            }
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
            var current = _playerPlaylist.GetCurrentIndex();
            if (current == -1) return;
            _playerPlaylist.Next();
            var next = _playerPlaylist.GetCurrentIndex();

            _playerPlaylist.Music[current].CurrentlyPlaying = false;

            if (!(next == 0 && !MainPage.Model.RepeatAll)) {
                if (_mediaPlayer.Source is MediaSource source) {
                    source.Dispose();
                }
                _playerPlaylist.Music[next].CurrentlyPlaying = true;
                _mediaPlayer.Source = MediaSource.CreateFromStorageFile(_playerPlaylist.Music[next].StorageFile);
                _mediaPlayer.Play();
            } else {
                Stop();
            }
        }

        public void PreviousTrack() {
            var current = _playerPlaylist.GetCurrentIndex();
            if (current == -1) return;
            _playerPlaylist.Previous();
            var previous = _playerPlaylist.GetCurrentIndex();

            if (_mediaPlayer.Source is MediaSource source) {
                source.Dispose();
            }

            _playerPlaylist.Music[current].CurrentlyPlaying = false;
            _playerPlaylist.Music[previous].CurrentlyPlaying = true;
            _mediaPlayer.Source = MediaSource.CreateFromStorageFile(_playerPlaylist.Music[previous].StorageFile);
            _mediaPlayer.Play();
        }

        public void Stop() {
            _mediaPlayer.Pause();
            if (_mediaPlayer.Source is MediaSource source) {
                source.Dispose();
            }
            _mediaPlayer.Source = null;
            _playerPlaylist.Music[_playerPlaylist.GetCurrentIndex()].CurrentlyPlaying = false;
            MainPage.Model.CurrentSeconds = 0;
            MainPage.Model.CurrentlyPlaying = "Nothing is playing...";
        }

        public void ClearPlaylist() {
            Stop();
            _playerPlaylist.Clear();
        }

        public void RepeatOne(bool value) {
            _mediaPlayer.IsLoopingEnabled = value;
        }

        public void Shuffle() {
            _playerPlaylist.Shuffle();
        }

        public Playlist GetPlaylist() {
            return _playerPlaylist;
        }
    }
}
