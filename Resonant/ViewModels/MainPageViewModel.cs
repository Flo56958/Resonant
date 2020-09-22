using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.Helpers;
using Resonant.Annotations;

namespace Resonant.ViewModels {
    public class MainPageViewModel : INotifyPropertyChanged {

        public double Progress => _currentSecond / _musicLengthSeconds * 100;

        public string TimeStamp
        {
            get
            {
                var currSec = (int) Math.Floor(CurrentSeconds % 60);
                var currSecString = currSec.ToString();
                if (currSec < 10) {
                    currSecString = 0 + currSecString;
                }

                var maxSec = (int) Math.Floor(MusicLengthSeconds % 60);
                var maxSecString = maxSec.ToString();
                if (maxSec < 10) {
                    maxSecString = 0 + maxSecString;
                }
                return $"{(int) Math.Floor(CurrentSeconds / 60)}:{currSecString} - {(int)Math.Floor(MusicLengthSeconds / 60)}:{maxSecString}";
            }
        }

        public double MusicLengthSeconds
        {
            get => _musicLengthSeconds;
            set {
                _musicLengthSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(TimeStamp));
            }
        }

        public double CurrentSeconds
        {
            get => _currentSecond;
            set
            {
                _currentSecond = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(TimeStamp));
            }
        }

        public string CurrentlyPlaying
        {
            get => _currentlyPlaying;
            set
            {
                _currentlyPlaying = value;
                OnPropertyChanged();
            }
        }

        private double _musicLengthSeconds;
        private double _currentSecond;
        private string _currentlyPlaying = "Nothing is playing...";

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            DispatcherHelper.ExecuteOnUIThreadAsync(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
