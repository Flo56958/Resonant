﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Windows.Data.Json;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;
using Genius;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Resonant.Annotations;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Resonant.Player {
    public class MusicFile : INotifyPropertyChanged {
        public enum MusicFileType {
            File,
            YouTube
        }
        private const string _loading = "...";

        private StorageFile _StorageFile = null;
        private string _YoutubeID = null;
        public BitmapImage Thumbnail { get; private set; }

        public BitmapImage ThumbnailBig { get; private set; }

        public MusicFileType Type { get; private set; }

        public bool CurrentlyPlaying
        {
            get => _currentlyPlaying;
            set
            {
                _currentlyPlaying = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Album {
            get => _album;
            set {
                _album = value;
                OnPropertyChanged();
            }
        }
        public string Artist {
            get => _artist;
            set {
                _artist = value;
                OnPropertyChanged();
            }
        }
        public string Bitrate {
            get => _bitrate;
            set {
                _bitrate = value;
                OnPropertyChanged();
            }
        }
        public string Duration {
            get => _duration;
            set {
                _duration = value;
                OnPropertyChanged();
            }
        }

        public string Path
        {
            get
            {
                switch (Type) {
                    case MusicFileType.File:
                        return _StorageFile.Path;
                    case MusicFileType.YouTube:
                        return _YoutubeID;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public string Lyrics { get; private set; }

        private string _title = _loading;
        private string _album = _loading;
        private string _artist = _loading;
        private string _bitrate = _loading;
        private string _duration = _loading;
        private bool _currentlyPlaying;

        public MusicFile(StorageFile file) {
            _StorageFile = file;
            Type = MusicFileType.File;
            Init();
        }

        public MusicFile(string ytID) {
            _YoutubeID = ytID;
            Type = MusicFileType.YouTube;
            Init();
        }

        private void Init() {
            Task.Run(() => {
                switch (Type) {
                    case MusicFileType.File: {
                        using var thumbnail = _StorageFile.GetThumbnailAsync(ThumbnailMode.MusicView, 45).GetAwaiter()
                            .GetResult();
                        if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon) return;
                        DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                        {
                            Thumbnail = new BitmapImage();
                            Thumbnail.SetSource(thumbnail);
                        }).GetAwaiter().GetResult();
                        OnPropertyChanged(nameof(Thumbnail));
                    } {
                        using var bigThumbnail = _StorageFile.GetThumbnailAsync(ThumbnailMode.MusicView, 300).GetAwaiter()
                            .GetResult();
                        if (bigThumbnail == null || bigThumbnail.Type == ThumbnailType.Icon) return;
                        DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                        {
                            ThumbnailBig = new BitmapImage();
                            ThumbnailBig.SetSource(bigThumbnail);
                        }).GetAwaiter().GetResult();
                        OnPropertyChanged(nameof(ThumbnailBig));
                    }
                        break;
                    case MusicFileType.YouTube:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
            Task.Run(() => {
                switch (Type) {
                    case MusicFileType.File:
                        var properties = _StorageFile.Properties.GetMusicPropertiesAsync().GetAwaiter().GetResult();
                        Title = properties.Title;
                        if (Title == null || Title == "") {
                            Title = _StorageFile.DisplayName;
                        }
                        Album = properties.Album;
                        Artist = properties.Artist;
                        Bitrate = properties.Bitrate + "bps";
                        Duration = properties.Duration.ToString().Split(".")[0];

                        if (properties.Subtitle.Equals("")) {
                            if (MainPage.Model.APIKey != null) {
                                var client = new GeniusClient(MainPage.Model.APIKey);
                                var hits = client.SearchClient.Search(properties.Title).GetAwaiter().GetResult().Response.Hits;
                                foreach (var hit in hits) {
                                    if (hit.Result.PrimaryArtist.Name.Equals(properties.Artist)) {
                                        //TODO: Add better Thumbnail Management
                                        if (ThumbnailBig == null) {
                                            DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                                                ThumbnailBig = new BitmapImage(new Uri(hit.Result.SongArtImageThumbnailUrl)));
                                            OnPropertyChanged(nameof(ThumbnailBig));
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
                        } else {
                            Lyrics = properties.Subtitle;
                        }
                        break;
                    case MusicFileType.YouTube:
                        var data = GetYTJSONData();
                        Title = data["videoDetails"]["title"].ToString();

                        Duration = new TimeSpan(0, 0, int.Parse(data["videoDetails"]["lengthSeconds"].ToString())).ToString().Split(".")[0];
                        Artist = data["videoDetails"]["author"].ToString();

                        var nfi = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };
                        Bitrate = int.Parse(data["videoDetails"]["viewCount"].ToString()).ToString("#,##0", nfi) + " views";
                        Album = "";
                        Lyrics = data["videoDetails"]["shortDescription"].ToString();

                        var arr = (JArray)data["videoDetails"]["thumbnail"]["thumbnails"];
                        if (arr.Count > 0) {
                            var littleURL = ((JObject)arr[0])["url"].ToString();
                            var bigURL = ((JObject)arr[arr.Count - 1])["url"].ToString();
                            DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                                Thumbnail = new BitmapImage(new Uri(littleURL)));
                            OnPropertyChanged(nameof(Thumbnail));
                            DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                                ThumbnailBig = new BitmapImage(new Uri(bigURL)));
                            OnPropertyChanged(nameof(ThumbnailBig));
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                using var source = GetSource();
                if (source == null) {
                    Title = "[UNAVAILABLE] " + Title;
                }
            });
        }

        public MediaSource GetSource() {
            switch (Type) {
                case MusicFileType.File:
                    return MediaSource.CreateFromStorageFile(_StorageFile);
                case MusicFileType.YouTube:
                {
                    //var client = new YoutubeClient();
                    //var mani = client.Videos.Streams.GetManifestAsync(_YoutubeID);
                    //var manii = mani.GetAwaiter().GetResult();
                    //var urll = manii.GetAudioOnly().First().Url;
                    //Debug.WriteLine(urll);
                    //return MediaSource.CreateFromUri(new Uri(urll));

                    var obj = GetYTJSONData();
                    if (!obj.ContainsKey("playabilityStatus")) return null;
                    if (!obj["playabilityStatus"]["status"].ToString().Equals("OK")) return null;
                    if (!obj.ContainsKey("streamingData")) return null;
                    var sdata = (JObject) obj["streamingData"];
                    if (!sdata.ContainsKey("formats")) return null;
                    var farr = (JArray) sdata["formats"];
                    if (farr.Count <= 0) return null;
                    foreach (var o in farr) {
                        var jo = (JObject) o;
                        string url;
                        if (jo.ContainsKey("url")) {
                            url = jo["url"].ToString();
                        } else if (jo.ContainsKey("signatureCipher")) {
                            url = jo["signatureCipher"].ToString();


                        } else continue;

                        url = HttpUtility.UrlDecode(url).Replace("\\u0026", "&");
                        Debug.WriteLine(url);
                        return MediaSource.CreateFromUri(new Uri(url));
                    }

                    return null;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private JObject GetYTJSONData() {
            if (Type != MusicFileType.YouTube) return null;
            var webRequest = WebRequest.Create(@"http://youtube.com/get_video_info?video_id=" + _YoutubeID +
                                               "&asv=2&el=detailpage");

            using var response = webRequest.GetResponse();
            using var content = response.GetResponseStream();
            using var reader = new StreamReader(content);
            var strContent = reader.ReadToEnd();

            strContent = HttpUtility.UrlDecode(strContent);

            //Extract valid json
            var idx = strContent.IndexOf("{");
            if (idx != -1) {
                var counter = 0;
                var idx2 = idx;
                do {
                    switch (strContent[idx2]) {
                        case '{':
                            counter++;
                            break;
                        case '}':
                            counter--;
                            break;
                    }

                    idx2++;
                } while (counter > 0 && idx2 < strContent.Length);

                idx2--;
                strContent = strContent.Substring(idx, idx2 - idx + 1);
            }

            return (JObject) JsonConvert.DeserializeObject(strContent);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            DispatcherHelper.ExecuteOnUIThreadAsync(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
