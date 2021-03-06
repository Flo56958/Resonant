﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.Uwp.Helpers;
using Resonant.Annotations;
using Resonant.Player;

namespace Resonant.ViewModels {
    public class MainPageViewModel : INotifyPropertyChanged {

        public bool RepeatAll
        {
            get => _repeatAll;
            set
            {
                _repeatAll = value;
                OnPropertyChanged();
            }
        }

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

        public MusicFile CurrentMusicFile
        {
            get => _currentMusicFile;
            set
            {
                _currentMusicFile = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MusicFile> CurrentMusicPlaylist => MusicController.GetMusicController().GetPlaylist().Music;

        public string APIKey
        {
            get => _APIKey;
            set
            {
                _APIKey = value;
                OnPropertyChanged();
            }
        }

        private bool _repeatAll;
        private double _musicLengthSeconds = 60;
        private double _currentSecond;
        private MusicFile _currentMusicFile;
        private string _APIKey;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            DispatcherHelper.ExecuteOnUIThreadAsync(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
