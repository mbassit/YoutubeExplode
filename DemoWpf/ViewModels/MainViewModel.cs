/* [5-May-2018, first time ever updated my forked GitHub repo with original author's repo] 
 * NB: To update my forked GitHub repo with the work done by the original author in his repo, I followed this tutorial: https://help.github.com/articles/syncing-a-fork
 * Basically:
 * 1) CD intro your forked repo
 * 2) Add the original author's repo as a new remote repository called "upstream" (sintax is "git remote add <shortname> <url>", e.g. "git remote add upstream https://github.com/ORIGINAL_OWNER/ORIGINAL_REPOSITORY.git"
 * 3) Fetch the remote repo called "upstream": git fetch upstream (this will create a local branch called "upstream/master"). NB: tried a normal pull from TortoiseGit and it works,
 *    just select "upstream" in the Remote drop down in the Pull dialog, which logs this: git.exe pull --progress -v --no-rebase "upstream" master
 * 4) Now merge "upstream/master" into whatever you want, eg. your forked repo master branch (and then push if you want this to be sent to your remote branch on GitHub)
 * TODO: merge from my master to myCustomizations branch 
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using Tyrrrz.Extensions;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;

namespace DemoWpf.ViewModels
{
    public class MainViewModel : ViewModelBase, IMainViewModel
    {
        private readonly YoutubeClient _client;

        private bool _isBusy;
        private string _query = "https://www.youtube.com/watch?v=dRZv355zZ1g";
        private Video _video;
        private Channel _channel;
        private MediaStreamInfoSet _mediaStreamInfos;
        private IReadOnlyList<ClosedCaptionTrackInfo> _closedCaptionTrackInfos;
        private double _progress;
        private bool _isProgressIndeterminate;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                Set(ref _isBusy, value);
                GetDataCommand.RaiseCanExecuteChanged();
                DownloadMediaStreamCommand.RaiseCanExecuteChanged();
            }
        }

        public string Query
        {
            get => _query;
            set
            {
                Set(ref _query, value);
                GetDataCommand.RaiseCanExecuteChanged();
            }
        }

        public Video Video
        {
            get => _video;
            private set
            {
                Set(ref _video, value);
                RaisePropertyChanged(() => IsDataAvailable);
            }
        }

        public Channel Channel
        {
            get => _channel;
            private set
            {
                Set(ref _channel, value);
                RaisePropertyChanged(() => IsDataAvailable);
            }
        }

        public MediaStreamInfoSet MediaStreamInfos
        {
            get => _mediaStreamInfos;
            private set
            {
                Set(ref _mediaStreamInfos, value);
                RaisePropertyChanged(() => IsDataAvailable);
            }
        }

        public IReadOnlyList<ClosedCaptionTrackInfo> ClosedCaptionTrackInfos
        {
            get => _closedCaptionTrackInfos;
            private set
            {
                Set(ref _closedCaptionTrackInfos, value);
                RaisePropertyChanged(() => IsDataAvailable);
            }
        }

        public bool IsDataAvailable => Video != null && Channel != null
                                       && MediaStreamInfos != null && ClosedCaptionTrackInfos != null;

        public double Progress
        {
            get => _progress;
            private set => Set(ref _progress, value);
        }

        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            private set => Set(ref _isProgressIndeterminate, value);
        }

        // Commands
        public RelayCommand GetDataCommand { get; }
        public RelayCommand<MediaStreamInfo> DownloadMediaStreamCommand { get; }
        public RelayCommand<ClosedCaptionTrackInfo> DownloadClosedCaptionTrackCommand { get; }

        public MainViewModel()
        {
            _client = new YoutubeClient();

            // Commands
            GetDataCommand = new RelayCommand(GetData,
                () => !IsBusy && Query.IsNotBlank());
            DownloadMediaStreamCommand = new RelayCommand<MediaStreamInfo>(DownloadMediaStream,
                _ => !IsBusy);
            DownloadClosedCaptionTrackCommand = new RelayCommand<ClosedCaptionTrackInfo>(
                DownloadClosedCaptionTrack, _ => !IsBusy);
        }

        private async void GetData()
        {
            try
            {
                IsBusy = true;
                IsProgressIndeterminate = true;

                // Reset data
                Video = null;
                Channel = null;
                MediaStreamInfos = null;
                ClosedCaptionTrackInfos = null;

                // Parse URL if necessary
                if (!YoutubeClient.TryParseVideoId(Query, out var videoId))
                    videoId = Query;

                // Get data
                Video = await _client.GetVideoAsync(videoId);
                Channel = await _client.GetVideoAuthorChannelAsync(videoId);
                MediaStreamInfos = await _client.GetVideoMediaStreamInfosAsync(videoId);
                ClosedCaptionTrackInfos = await _client.GetVideoClosedCaptionTrackInfosAsync(videoId);

                IsBusy = false;
                IsProgressIndeterminate = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                IsProgressIndeterminate = false;
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DownloadMediaStream(MediaStreamInfo info)
        {
            // Create dialog
            var fileExt = info.Container.GetFileExtension();
            var defaultFileName = $"{Video.Title}.{fileExt}"
                .Replace(Path.GetInvalidFileNameChars(), '_');
            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = fileExt,
                FileName = defaultFileName,
                Filter = $"{info.Container} Files|*.{fileExt}|All Files|*.*"
            };

            // Select file path
            if (sfd.ShowDialog() != true)
                return;

            var filePath = sfd.FileName;

            // Download to file
            IsBusy = true;
            Progress = 0;

            var progressHandler = new Progress<double>(p => Progress = p);
            await _client.DownloadMediaStreamAsync(info, filePath, progressHandler);
            System.Media.SystemSounds.Asterisk.Play();
            IsBusy = false;
            Progress = 0;
        }

        private async void DownloadClosedCaptionTrack(ClosedCaptionTrackInfo info)
        {
            // Create dialog
            var fileExt = $"{Video.Title}.{info.Language.Name}.srt"
                .Replace(Path.GetInvalidFileNameChars(), '_');
            var filter = "SRT Files|*.srt|All Files|*.*";
            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "srt",
                FileName = fileExt,
                Filter = filter
            };

            // Select file path
            if (sfd.ShowDialog() != true)
                return;

            var filePath = sfd.FileName;

            // Download to file
            IsBusy = true;
            Progress = 0;

            var progressHandler = new Progress<double>(p => Progress = p);
            await _client.DownloadClosedCaptionTrackAsync(info, filePath, progressHandler);

            IsBusy = false;
            Progress = 0;
        }
    }
}