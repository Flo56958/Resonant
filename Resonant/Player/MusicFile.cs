using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.Helpers;
using Resonant.Annotations;

namespace Resonant.Player {
    public class MusicFile : INotifyPropertyChanged {
        private const string _loading = "...";

        public StorageFile StorageFile { get; }
        public BitmapImage Thumbnail { get; private set; }

        public BitmapImage ThumbnailBig { get; private set; }

        public bool CurrentlyPlaying
        {
            get => _currentlyPlaying;
            set
            {
                _currentlyPlaying = value;
                if (value) {
                    Task.Run(() => {
                        using var thumbnail = StorageFile.GetThumbnailAsync(ThumbnailMode.MusicView, 300).GetAwaiter().GetResult();
                        if (thumbnail == null) return;
                        DispatcherHelper.ExecuteOnUIThreadAsync(() => {
                            ThumbnailBig = new BitmapImage();
                            ThumbnailBig.SetSource(thumbnail);
                        }).GetAwaiter().GetResult();
                        OnPropertyChanged(nameof(ThumbnailBig));
                    });
                }
                else {
                    ThumbnailBig = null;
                    OnPropertyChanged(nameof(ThumbnailBig));
                }
                OnPropertyChanged();
            }
        }

        public string Title => _properties != null ? _properties.Title : _loading;

        public string Album => _properties != null ? _properties.Album : _loading;

        public string Artist => _properties != null ? _properties.Artist : _loading;

        public string Bitrate => (_properties != null) ? _properties.Bitrate + "bps" : _loading;

        public string Duration => _properties != null ? _properties.Duration.ToString().Split(".")[0] : _loading;

        private MusicProperties _properties;
        private bool _currentlyPlaying;

        public MusicFile(StorageFile file) {
            StorageFile = file;

            Task.Run(() => {
                using var thumbnail = file.GetThumbnailAsync(ThumbnailMode.MusicView, 45).GetAwaiter().GetResult();
                if (thumbnail == null) return;
                DispatcherHelper.ExecuteOnUIThreadAsync(() => {
                    Thumbnail = new BitmapImage();
                    Thumbnail.SetSource(thumbnail);
                }).GetAwaiter().GetResult();
                OnPropertyChanged(nameof(Thumbnail));
            });
            Task.Run(() => {
                _properties = file.Properties.GetMusicPropertiesAsync().GetAwaiter().GetResult();
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Album));
                OnPropertyChanged(nameof(Artist));
                OnPropertyChanged(nameof(Bitrate));
                OnPropertyChanged(nameof(Duration));
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            DispatcherHelper.ExecuteOnUIThreadAsync(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
