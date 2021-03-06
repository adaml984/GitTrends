﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using GitTrends.Mobile.Shared;
using GitTrends.Shared;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace GitTrends
{
    public class SettingsViewModel : GitHubAuthenticationViewModel
    {
        readonly GitHubAuthenticationService _gitHubAuthenticationService;
        readonly TrendsChartSettingsService _trendsChartSettingsService;
        readonly DeepLinkingService _deepLinkingService;
        readonly NotificationService _notificationService;
        readonly ThemeService _themeService;

        string _gitHubUserImageSource = string.Empty;
        string _gitHubUserNameLabelText = string.Empty;
        string _gitHubNameLabelText = string.Empty;
        string _gitHubButtonText = string.Empty;
        bool _isRegisterForNotificationsSwitchEnabled = true;
        bool _isRegisterForNotificationsSwitchToggled;

        int _themePickerSelectedThemeIndex;

        public SettingsViewModel(GitHubAuthenticationService gitHubAuthenticationService,
                                    ThemeService themeService,
                                    TrendsChartSettingsService trendsChartSettingsService,
                                    AnalyticsService analyticsService,
                                    DeepLinkingService deepLinkingService,
                                    NotificationService notificationService)
                : base(gitHubAuthenticationService, deepLinkingService, analyticsService)
        {
            _gitHubAuthenticationService = gitHubAuthenticationService;
            _trendsChartSettingsService = trendsChartSettingsService;
            _deepLinkingService = deepLinkingService;
            _notificationService = notificationService;
            _themeService = themeService;

            CopyrightLabelTappedCommand = new AsyncCommand(ExecuteCopyrightLabelTappedCommand);
            GitHubUserViewTappedCommand = new AsyncCommand(ExecuteGitHubUserViewTappedCommand, _ => GitHubAuthenticationService.IsAuthenticated || GitHubAuthenticationService.IsDemoUser);

            _gitHubAuthenticationService.AuthorizeSessionCompleted += HandleAuthorizeSessionCompleted;
            ThemeService.PreferenceChanged += HandlePreferenceChanged;

            ThemePickerSelectedThemeIndex = (int)themeService.Preference;

            var app = (App)Application.Current;
            app.Resumed += HandleResumed;

            initializeIsRegisterForNotificationsSwitch().SafeFireAndForget();

            SetGitHubValues();

            async Task initializeIsRegisterForNotificationsSwitch() => IsRegisterForNotificationsSwitchToggled = notificationService.ShouldSendNotifications && await notificationService.AreNotificationsEnabled().ConfigureAwait(false);
        }

        public ICommand CopyrightLabelTappedCommand { get; }
        public ICommand GitHubUserViewTappedCommand { get; }
        public IReadOnlyList<string> ThemePickerItemsSource { get; } = Enum.GetNames(typeof(PreferredTheme));

        public bool IsAliasLabelVisible => !IsAuthenticating && LoginLabelText is GitHubLoginButtonConstants.Disconnect;
        public override bool IsDemoButtonVisible => base.IsDemoButtonVisible && LoginLabelText is GitHubLoginButtonConstants.ConnectToGitHub;

        public bool ShouldShowClonesByDefaultSwitchValue
        {
            get => _trendsChartSettingsService.ShouldShowClonesByDefault;
            set
            {
                _trendsChartSettingsService.ShouldShowClonesByDefault = value;
                OnPropertyChanged();
            }
        }

        public bool ShouldShowUniqueClonesByDefaultSwitchValue
        {
            get => _trendsChartSettingsService.ShouldShowUniqueClonesByDefault;
            set
            {
                _trendsChartSettingsService.ShouldShowUniqueClonesByDefault = value;
                OnPropertyChanged();
            }
        }

        public bool ShouldShowViewsByDefaultSwitchValue
        {
            get => _trendsChartSettingsService.ShouldShowViewsByDefault;
            set
            {
                _trendsChartSettingsService.ShouldShowViewsByDefault = value;
                OnPropertyChanged();
            }
        }

        public bool ShouldShowUniqueViewsByDefaultSwitchValue
        {
            get => _trendsChartSettingsService.ShouldShowUniqueViewsByDefault;
            set
            {
                _trendsChartSettingsService.ShouldShowUniqueViewsByDefault = value;
                OnPropertyChanged();
            }
        }

        public string LoginLabelText
        {
            get => _gitHubButtonText;
            set => SetProperty(ref _gitHubButtonText, value, () =>
            {
                OnPropertyChanged(nameof(IsDemoButtonVisible));
                OnPropertyChanged(nameof(IsAliasLabelVisible));
            });
        }

        public string GitHubAvatarImageSource
        {
            get => _gitHubUserImageSource;
            set => SetProperty(ref _gitHubUserImageSource, value);
        }

        public string GitHubAliasLabelText
        {
            get => _gitHubUserNameLabelText;
            set => SetProperty(ref _gitHubUserNameLabelText, value);
        }

        public string GitHubNameLabelText
        {
            get => _gitHubNameLabelText;
            set => SetProperty(ref _gitHubNameLabelText, value);
        }

        public int ThemePickerSelectedThemeIndex
        {
            get => _themePickerSelectedThemeIndex;
            set => SetProperty(ref _themePickerSelectedThemeIndex, value, () => _themeService.Preference = (PreferredTheme)value);
        }

        public bool IsRegisterForNotificationsSwitchEnabled
        {
            get => _isRegisterForNotificationsSwitchEnabled;
            set => SetProperty(ref _isRegisterForNotificationsSwitchEnabled, value);
        }

        public bool IsRegisterForNotificationsSwitchToggled
        {
            get => _isRegisterForNotificationsSwitchToggled;
            set => SetProperty(ref _isRegisterForNotificationsSwitchToggled, value, async () => await SetNotificationsPreference(value).ConfigureAwait(false));
        }

        protected override void NotifyIsAuthenticatingPropertyChanged()
        {
            base.NotifyIsAuthenticatingPropertyChanged();
            OnPropertyChanged(nameof(IsAliasLabelVisible));
        }

        protected override async Task ExecuteConnectToGitHubButtonCommand(GitHubAuthenticationService gitHubAuthenticationService, DeepLinkingService deepLinkingService, CancellationToken cancellationToken, BrowserLaunchOptions? browserLaunchOptions)
        {
            AnalyticsService.Track("Login Button Tapped", nameof(GitHubAuthenticationService.IsAuthenticated), gitHubAuthenticationService.IsAuthenticated.ToString());

            if (gitHubAuthenticationService.IsAuthenticated)
            {
                await gitHubAuthenticationService.LogOut().ConfigureAwait(false);

                SetGitHubValues();
            }
            else
            {
                await base.ExecuteConnectToGitHubButtonCommand(gitHubAuthenticationService, deepLinkingService, cancellationToken, browserLaunchOptions).ConfigureAwait(false);
            }
        }

        protected override async Task ExecuteDemoButtonCommand(string buttonText)
        {
            try
            {
                await base.ExecuteDemoButtonCommand(buttonText).ConfigureAwait(false);

                AnalyticsService.Track("Settings Try Demo Button Tapped");
                await _gitHubAuthenticationService.ActivateDemoUser().ConfigureAwait(false);
                SetGitHubValues();
            }
            finally
            {
                IsAuthenticating = false;
            }
        }

        async Task SetNotificationsPreference(bool isNotificationsEnabled)
        {
            IsRegisterForNotificationsSwitchEnabled = false;

            try
            {
                if (isNotificationsEnabled)
                {
                    var result = await _notificationService.Register(true).ConfigureAwait(false);
                    AnalyticsService.Track("Settings Notification Changed", new Dictionary<string, string>
                    {
                        { nameof(isNotificationsEnabled), isNotificationsEnabled.ToString() },
                        { "Result", result.ToString()}
                    });
                }
                else
                {
                    _notificationService.UnRegister();
                    AnalyticsService.Track("Settings Notification Button Tapped", nameof(isNotificationsEnabled), isNotificationsEnabled.ToString());
                }
            }
            finally
            {
                IsRegisterForNotificationsSwitchEnabled = true;
            }
        }


        Task ExecuteCopyrightLabelTappedCommand()
        {
            AnalyticsService.Track("CreatedBy Label Tapped");
            return _deepLinkingService.OpenApp("twitter://user?id=3418408341", "https://twitter.com/intent/user?user_id=3418408341");
        }


        void HandleAuthorizeSessionCompleted(object sender, AuthorizeSessionCompletedEventArgs e) => SetGitHubValues();

        async void HandleResumed(object sender, EventArgs e)
        {
            IsRegisterForNotificationsSwitchToggled = _notificationService.ShouldSendNotifications && await _notificationService.AreNotificationsEnabled().ConfigureAwait(false);
            IsRegisterForNotificationsSwitchEnabled = true;
        }

        void HandlePreferenceChanged(object sender, PreferredTheme e)
        {
            if (!_gitHubAuthenticationService.IsAuthenticated)
                GitHubAvatarImageSource = BaseTheme.GetDefaultProfileImageSource();
            else if (GitHubAuthenticationService.Alias is DemoDataConstants.Alias)
                GitHubAvatarImageSource = BaseTheme.GetGitTrendsImageSource();
        }

        void SetGitHubValues()
        {
            GitHubAliasLabelText = _gitHubAuthenticationService.IsAuthenticated ? $"@{GitHubAuthenticationService.Alias}" : string.Empty;
            GitHubNameLabelText = _gitHubAuthenticationService.IsAuthenticated ? GitHubAuthenticationService.Name : GitHubLoginButtonConstants.NotLoggedIn;
            LoginLabelText = _gitHubAuthenticationService.IsAuthenticated ? $"{GitHubLoginButtonConstants.Disconnect}" : $"{GitHubLoginButtonConstants.ConnectToGitHub}";
            GitHubAvatarImageSource = _gitHubAuthenticationService.IsAuthenticated ? GitHubAuthenticationService.AvatarUrl : BaseTheme.GetDefaultProfileImageSource();
        }

        Task ExecuteGitHubUserViewTappedCommand()
        {
            string alias = GitHubAuthenticationService.Alias is DemoDataConstants.Alias ? nameof(GitTrends) : GitHubAuthenticationService.Alias;
            AnalyticsService.Track("Alias Label Tapped", "Alias", alias);

            return _deepLinkingService.OpenApp($"github://", $"{GitHubConstants.GitHubBaseUrl}/{alias}", $"{GitHubConstants.GitHubBaseUrl}/{alias}");
        }
    }
}
