using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Resonant.Player;
using Resonant.ViewModels;

namespace Resonant
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage {

        public static MainPageViewModel Model { get; private set; }
        public static MainPage Window { get; private set; }
        public ListView CurrentlyPlayingListView { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            _init_AudioDeviceSelector();
            if (DataContext == null) {
                DataContext = new MainPageViewModel();
            }
            Model = (MainPageViewModel) DataContext;
            Window = this;
            CurrentlyPlayingListView = CurrentlyPlaying;
        }

        private async void _init_AudioDeviceSelector() {
            var audioSelector = MediaDevice.GetAudioRenderSelector();
            var outputDevices = await DeviceInformation.FindAllAsync(audioSelector);
            foreach (var device in outputDevices) {
                var deviceItem = new ComboBoxItem { Content = device.Name, Tag = device };
                AudioDeviceSelector.Items?.Add(deviceItem);
            }
        }

        private void AudioDeviceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selectedDevice = (DeviceInformation)((ComboBoxItem)AudioDeviceSelector.SelectedItem)?.Tag;
            if (selectedDevice != null) {
                MusicController.GetMusicController().SetAudioDevice(selectedDevice);
            }
        }

        private async void Add_Music_Button_Click(object sender, RoutedEventArgs e) {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            openPicker.FileTypeFilter.Add(".mp3");
            openPicker.FileTypeFilter.Add(".wav");

            var files = await openPicker.PickMultipleFilesAsync();
            if (files != null) {
                MusicController.GetMusicController().AddMusic(files);
            }
        }

        private void Play_Pause_Button_Click(object sender, RoutedEventArgs e) {
            MusicController.GetMusicController().TogglePlayPause();
        }

        private void Speed_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {
            MusicController.GetMusicController().SetSpeed(e.NewValue);
        }

        private void Volume_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {
            MusicController.GetMusicController().SetVolume(e.NewValue / 100);
        }

        private void AudioBalance_Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {
            MusicController.GetMusicController().SetAudioBalance(e.NewValue);
        }

        private void Previous_Button_Click(object sender, RoutedEventArgs e) {
            MusicController.GetMusicController().PreviousTrack();
        }

        private void Next_Button_Click(object sender, RoutedEventArgs e) {
            MusicController.GetMusicController().NextTrack();
        }

        private void RepeatOne_AppBarToggleButton_Checked(object sender, RoutedEventArgs e) {
            MusicController.GetMusicController().RepeatOne(true);
        }

        private void RepeatOne_AppBarToggleButton_Unchecked(object sender, RoutedEventArgs e) {
            MusicController.GetMusicController().RepeatOne(false);
        }

        private void Shuffle_ButtonBase_OnClick(object sender, RoutedEventArgs e) {
            MusicController.GetMusicController().Shuffle();
        }

        private void ClearAll_AppBarToggleButton_OnClick(object sender, RoutedEventArgs e) {
            MusicController.GetMusicController().ClearPlaylist();
        }

        private void Sort_ButtonBase_OnClick(object sender, RoutedEventArgs e) {
            MusicController.GetMusicController().GetPlaylist().Sort();
        }
    }
}
