using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using ColorCode.Common;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Resonant.Player {
    internal class Playlist {

        public readonly ObservableCollection<MusicFile> Music = new ObservableCollection<MusicFile>();

        private int _current;

        public void AddMusicFile(MusicFile file) {
            DispatcherHelper.ExecuteOnUIThreadAsync(() => Music.Add(file));
        }

        public void Clear() {
            _current = 0;
            Music.Clear();
        }

        public void Sort() {
            var current = Music[_current];
            Music.SortStable((x,y) => string.Compare(x.Title, y.Title, StringComparison.Ordinal));
            _current = Music.IndexOf(current);
        }

        public void Shuffle() {
            using var provider = new RNGCryptoServiceProvider();
            var n = Music.Count;
            while (n > 1) {
                var box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (byte.MaxValue / n)));
                var k = (box[0] % n);
                n--;
                if (_current == k) _current = n;
                else if (_current == n) _current = k;
                var value = Music[k];
                Music[k] = Music[n];
                Music[n] = value;
            }
        }

        public int GetCurrentIndex() {
            if (Music.Count <= 0) return -1;
            return _current;
        }

        public int Previous() {
            if (Music.Count == 0) return -1;
            _current -= 1;
            if (_current < 0) _current = Music.Count - 1;

            return _current;
        }

        public int Next() {
            if (Music.Count == 0) return -1;
            _current = (_current + 1) % Music.Count;
            return _current;
        }
    }
}
