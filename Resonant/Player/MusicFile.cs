using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Genius;
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
                        if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon) return;
                        DispatcherHelper.ExecuteOnUIThreadAsync(() => {
                            ThumbnailBig = new BitmapImage();
                            ThumbnailBig.SetSource(thumbnail);
                        }).GetAwaiter().GetResult();
                        OnPropertyChanged(nameof(ThumbnailBig));
                    });
                }
                else {
                    //ThumbnailBig = null;
                    OnPropertyChanged(nameof(ThumbnailBig));
                }
                OnPropertyChanged();
            }
        }

        public string Title => _properties != null ? (_properties.Title.Equals("") ? StorageFile.DisplayName : _properties.Title) : _loading;

        public string Album => _properties != null ? _properties.Album : _loading;

        public string Artist => _properties != null ? _properties.Artist : _loading;

        public string Bitrate => (_properties != null) ? _properties.Bitrate + "bps" : _loading;

        public string Duration => _properties != null ? _properties.Duration.ToString().Split(".")[0] : _loading;

        public string Lyrics { get; private set; }

        private MusicProperties _properties;
        private bool _currentlyPlaying;

        public MusicFile(StorageFile file) {
            StorageFile = file;

            Task.Run(() => {
                using var thumbnail = file.GetThumbnailAsync(ThumbnailMode.MusicView, 45).GetAwaiter().GetResult();
                if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon) return;
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

                if (_properties.Subtitle.Equals("")) {
                    if (MainPage.Model.APIKey != null) {
                        var client = new GeniusClient(MainPage.Model.APIKey);
                        var hits = client.SearchClient.Search(_properties.Title).GetAwaiter().GetResult().Response.Hits;
                        foreach (var hit in hits) {
                            if (hit.Result.PrimaryArtist.Name.Equals(_properties.Artist)) {
                                //TODO: Add better Thumbnail Management
                                if (ThumbnailBig == null) {
                                    DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                                        ThumbnailBig = new BitmapImage(new Uri(hit.Result.SongArtImageThumbnailUrl)));
                                }
                                /* TODO: Implement better Lyrics scraper
                                using var httpClient = new HttpClient();
                                var request = httpClient.GetAsync(hit.Result.Url).GetAwaiter().GetResult();
                                var response = request.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

                                var parser = new HtmlParser();
                                var document = parser.ParseDocument(response);
                                
                                var d = document.All.Where(x => (x.LocalName?.Equals("div")).GetValueOrDefault(false) && !x.TextContent.Equals("") && x.InnerHtml.StartsWith("<div id=\"lyrics\" class=\""));
                                Debug.WriteLine(d.Count());
                                var sb = new StringBuilder();
                                foreach (var element in d) {
                                    var text = element.ParentElement.TextContent;
                                    sb.Append(text);
                                }

                                Lyrics = sb.ToString();
                                OnPropertyChanged(nameof(Lyrics));
                                */
                                break;
                            }
                        }
                    }
                }
                else {
                    Lyrics = _properties.Subtitle;
                }


            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            DispatcherHelper.ExecuteOnUIThreadAsync(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
