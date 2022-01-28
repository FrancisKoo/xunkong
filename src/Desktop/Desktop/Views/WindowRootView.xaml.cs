﻿using CommunityToolkit.WinUI.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Xunkong.Desktop.Pages;
using Xunkong.Desktop.Services;
using Xunkong.Desktop.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Xunkong.Desktop.Views
{
    public sealed partial class WindowRootView : UserControl
    {

        private WindowRootViewModel vm => (DataContext as WindowRootViewModel)!;

        public UIElement AppTitleBar => _appTitleBar;

        private readonly DbConnectionFactory<SqliteConnection> _dbConnectionFactory;

        private readonly ILogger<WindowRootView> _logger;


        public WindowRootView()
        {
            this.InitializeComponent();
            DataContext = App.Current.Services.GetService<WindowRootViewModel>();
            _dbConnectionFactory = App.Current.Services.GetService<DbConnectionFactory<SqliteConnection>>()!;
            _logger = App.Current.Services.GetService<ILogger<WindowRootView>>()!;
            Loaded += WindowRootView_Loaded;
            WeakReferenceMessenger.Default.Register<RefreshWebToolNavItemMessage>(this, async (_, _) => await RefreshWebToolNavItemAsync());
            WeakReferenceMessenger.Default.Register<NavigateMessage>(this, (_, m) => NavigateTo(m));
            WeakReferenceMessenger.Default.Register<OpenNavigationPanelMessage>(this, (_, _) => _NavigationView.IsPaneOpen = true); ;
        }

        private async void WindowRootView_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshWebToolNavItemAsync();
            await GetNotificationsAsync();
            await CheckWebView2Runtime();
        }




        #region NavigationViewTranslation


        private string GetAppTitleFromPackage()
        {
            return Package.Current.DisplayName;
        }


        private void _NavigationView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            Thickness currMargin = _appTitleBar.Margin;
            if (sender.DisplayMode == NavigationViewDisplayMode.Minimal)
            {
                _appTitleBar.Margin = new Thickness((sender.CompactPaneLength * 2), currMargin.Top, currMargin.Right, currMargin.Bottom);
                _NavigationView.IsPaneToggleButtonVisible = true;
            }
            else
            {
                _appTitleBar.Margin = new Thickness(sender.CompactPaneLength, currMargin.Top, currMargin.Right, currMargin.Bottom);
                _NavigationView.IsPaneToggleButtonVisible = false;
            }
            UpdateAppTitleMargin(sender);
        }


        private void _NavigationView_PaneOpening(NavigationView sender, object args)
        {
            UpdateAppTitleMargin(sender);
        }

        private void _NavigationView_PaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
        {
            UpdateAppTitleMargin(sender);
        }

        private void UpdateAppTitleMargin(NavigationView sender)
        {
            const int smallLeftIndent = 4, largeLeftIndent = 24;

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
            {
                AppTitle.TranslationTransition = new Vector3Transition();

                if ((sender.DisplayMode == NavigationViewDisplayMode.Expanded && sender.IsPaneOpen) ||
                         sender.DisplayMode == NavigationViewDisplayMode.Minimal)
                {
                    AppTitle.Translation = new System.Numerics.Vector3(smallLeftIndent, 0, 0);
                }
                else
                {
                    AppTitle.Translation = new System.Numerics.Vector3(largeLeftIndent, 0, 0);
                }
            }
            else
            {
                Thickness currMargin = AppTitle.Margin;

                if ((sender.DisplayMode == NavigationViewDisplayMode.Expanded && sender.IsPaneOpen) ||
                         sender.DisplayMode == NavigationViewDisplayMode.Minimal)
                {
                    AppTitle.Margin = new Thickness(smallLeftIndent, currMargin.Top, currMargin.Right, currMargin.Bottom);
                }
                else
                {
                    AppTitle.Margin = new Thickness(largeLeftIndent, currMargin.Top, currMargin.Right, currMargin.Bottom);
                }
            }
        }


        private void _NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }


        private bool TryGoBack()
        {
            if (_rootFrame.CanGoBack)
            {
                _rootFrame.GoBack();
                return true;
            }
            else
            {
                return false;
            }
        }


        #endregion




        private void ShowAttachedFlyout(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }


        private void _NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer.IsSelected)
            {
                return;
            }
            if (args.IsSettingsInvoked)
            {
                _logger.LogInformation("Navigate to {PageName}", "SettingPage");
                _rootFrame.Navigate(typeof(SettingPage));
            }
            else
            {
                if (args.InvokedItemContainer.DataContext is WebToolItem webToolItem)
                {
                    _logger.LogInformation("Navigate to {PageName} with title {Title} and url {Url}", "WebToolPage", webToolItem.Title, webToolItem.Url);
                    _rootFrame.Navigate(typeof(WebToolPage), webToolItem);
                }
                else
                {
                    var tag = args.InvokedItemContainer.Tag as string;
                    var asm = typeof(WindowRootView).Assembly;
                    var type = asm.GetType($"Xunkong.Desktop.Pages.{tag}");
                    if (type is not null)
                    {
                        _logger.LogInformation("Navigate to {PageName}", tag);
                        _rootFrame.Navigate(type);
                    }
                    else
                    {
                        _logger.LogWarning("Navigate to unfount page {PageName}", tag);
                    }
                }
            }
        }




        private void NavigateTo(NavigateMessage message)
        {
            _logger.LogInformation("Navigate to {PageName} with paramter {Parameter}", message.Type.Name, message.Parameter);
            _rootFrame.DispatcherQueue.TryEnqueue(() => _rootFrame.Navigate(message.Type, message.Parameter, message.TransitionInfo));
        }




        private async Task RefreshWebToolNavItemAsync()
        {
            const string WEBTOOL = "WebTool";
            try
            {
                var removing = _NavigationView.MenuItems.Where(x => (x as FrameworkElement)?.Tag as string == WEBTOOL).ToList();
                foreach (var item in removing)
                {
                    _NavigationView.MenuItems.Remove(item);
                }
                using var con = _dbConnectionFactory.CreateDbConnection();
                var sql = "SELECT * FROM WebToolItems ORDER BY [Order]";
                var list = await con.QueryAsync<WebToolItem>(sql);
                if (list.Any())
                {
                    _NavigationView.MenuItems.Add(new NavigationViewItemSeparator { Tag = WEBTOOL });
                    _NavigationView.MenuItems.Add(new NavigationViewItemHeader { Content = "小工具", Tag = WEBTOOL });
                    foreach (var item in list)
                    {
                        _NavigationView.MenuItems.Add(new NavigationViewItem
                        {
                            Icon = new BitmapIcon { UriSource = new Uri(item.Icon ?? ""), ShowAsMonochrome = false },
                            Content = item.Title,
                            DataContext = item,
                            Tag = WEBTOOL,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {MethodName}", nameof(RefreshWebToolNavItemAsync));
                InfoBarHelper.Error(ex, $"加载网页小工具");
            }
        }




        private async Task GetNotificationsAsync()
        {
            try
            {
                var channel = XunkongEnvironment.Channel;
                var version = XunkongEnvironment.AppVersion;
                var xunkongApiService = App.Current.Services.GetService<XunkongApiService>();
                var hasNew = await xunkongApiService!.GetNotificationsAsync(channel, version);
                if (hasNew)
                {
                    _Badge_Notification.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {

            }
        }



        private async Task CheckWebView2Runtime()
        {
            try
            {
                _ = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch
            {
                const string url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
                var button = new Button { Content = "下载", HorizontalAlignment = HorizontalAlignment.Right };
                button.Click += (_, _) => Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                });
                var infoBar = new InfoBar
                {
                    Severity = InfoBarSeverity.Warning,
                    Title = "警告",
                    Message = "没有找到WebView2运行时，会影响软件必要的功能。",
                    ActionButton = button,
                    IsOpen = true,
                };
                InfoBarHelper.Show(infoBar);
            }
        }





    }


}
