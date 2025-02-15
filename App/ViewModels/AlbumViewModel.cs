﻿using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Xunkong.Desktop.ViewModels;

internal partial class AlbumViewModel : ObservableObject
{

    private const string REG_PATH = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\原神\";
    private const string REG_KEY = "InstallPath";



    private string? albumFolder;

    private FileSystemWatcher _watcher;


    public AlbumViewModel()
    {
        _watcher = new();
        _watcher.Filter = "*.png";
        _watcher.Created += FileSystemWatcher_Created;
        _watcher.Deleted += FileSystemWatcher_Deleted;
    }


    private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                var fileInfo = new FileInfo(e.FullPath);
                if (fileInfo.Exists && fileInfo.Extension.ToLower() == ".png")
                {
                    var dq = MainWindow.Current.DispatcherQueue;
                    if (latestImages is null)
                    {
                        latestImages = new("新增", new[] { fileInfo });
                        dq.TryEnqueue(() => ImageGroupList?.Insert(0, latestImages));
                    }
                    else
                    {
                        dq.TryEnqueue(() => latestImages.Add(fileInfo));
                    }
                    dq.TryEnqueue(() => ImageList.Insert(latestImages.Count - 1, fileInfo));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "截图文件夹内有新文件");
        }
    }


    private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                var path = Path.GetFullPath(e.FullPath);
                var dq = MainWindow.Current.DispatcherQueue;
                dq.TryEnqueue(() =>
                {
                    var file1 = latestImages?.FirstOrDefault(x => x.FullName == path);
                    if (file1 is not null)
                    {
                        latestImages?.Remove(file1);
                    }
                    var file2 = ImageList?.FirstOrDefault(x => x.FullName == path);
                    if (file2 is not null)
                    {
                        ImageList?.Remove(file2);
                    }
                    foreach (var item in ImageGroupList)
                    {
                        var file3 = item.FirstOrDefault(x => x.FullName == path);
                        if (file3 is not null)
                        {
                            item.Remove(file3);
                            break;
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "截图文件夹内有文件被删除");
        }
    }


    private ImageGroupCollection latestImages;


    private ObservableCollection<ImageGroupCollection> _ImageGroupList;
    public ObservableCollection<ImageGroupCollection> ImageGroupList
    {
        get => _ImageGroupList;
        set => SetProperty(ref _ImageGroupList, value);
    }


    private ObservableCollection<FileInfo> _ImageList;
    public ObservableCollection<FileInfo> ImageList
    {
        get => _ImageList;
        set => SetProperty(ref _ImageList, value);
    }



    public async Task InitializeDataAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(albumFolder))
            {
                return;
            }
            await InitializeScreenFolderAsync();
            if (Directory.Exists(albumFolder))
            {
                _watcher.Path = albumFolder;
                _watcher.EnableRaisingEvents = true;
                GetAllImages();
            }
        }
        catch (Exception ex)
        {
            NotificationProvider.Error(ex, "初始化相册页面");
            Logger.Error(ex, "初始化相册页面");
        }
    }


    private async Task InitializeScreenFolderAsync()
    {
        if (AppSetting.TryGetValue<string>(SettingKeys.ScreenFolderPath, out var screenFolderPath))
        {
            if (Directory.Exists(screenFolderPath))
            {
                albumFolder = screenFolderPath;
                return;
            }
        }
        var launcherPath = Registry.GetValue(REG_PATH, REG_KEY, null) as string;
        if (!string.IsNullOrWhiteSpace(launcherPath))
        {
            var configPath = Path.Combine(launcherPath, "config.ini");
            if (File.Exists(configPath))
            {
                var str = await File.ReadAllTextAsync(configPath);
                var gamePath = Regex.Match(str, @"game_install_path=(.+)").Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(gamePath))
                {
                    screenFolderPath = Path.Combine(gamePath, "ScreenShot");
                    if (Directory.Exists(screenFolderPath))
                    {
                        albumFolder = screenFolderPath;
                        AppSetting.TrySetValue(SettingKeys.ScreenFolderPath, screenFolderPath);
                        return;
                    }
                }
            }
        }
        NotificationProvider.Warning("没有找到截图文件夹");
    }



    private void GetAllImages()
    {
        if (Directory.Exists(albumFolder))
        {
            var folderInfo = new DirectoryInfo(albumFolder);
            var fileInfos = folderInfo.GetFiles();
            var queryList = fileInfos.Where(x => x.Extension.ToLower() == ".png").OrderByDescending(x => x.CreationTime);
            ImageList = new(queryList);
            var queryGroup = ImageList.GroupBy(x => x.CreationTime.ToString("yyyy-MM")).Select(x => new ImageGroupCollection(x.Key, x)).OrderByDescending(x => x.Header);
            ImageGroupList = new(queryGroup);
        }
    }



    [RelayCommand]
    private void OpenImageFolder()
    {
        if (!Directory.Exists(albumFolder))
        {
            NotificationProvider.Warning("没有找到截图文件夹");
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = albumFolder,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            NotificationProvider.Error(ex, "打开截图文件夹");
            Logger.Error(ex, "打开截图文件夹");
        }
    }


    [RelayCommand]
    private async Task SetImageFolderAsync()
    {
        try
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, MainWindow.Current.HWND);
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder is not null)
            {
                albumFolder = folder.Path;
                _watcher.Path = albumFolder;
                _watcher.EnableRaisingEvents = true;
                AppSetting.SetValue(SettingKeys.ScreenFolderPath, albumFolder);
                GetAllImages();
            }
        }
        catch (Exception ex)
        {
            NotificationProvider.Error(ex, "设置截图文件夹");
            Logger.Error(ex, "设置截图文件夹");
        }
    }




    internal class ImageGroupCollection : ObservableCollection<FileInfo>
    {

        public string Header { get; set; }


        public ImageGroupCollection(string header, IEnumerable<FileInfo> list) : base(list)
        {
            Header = header;
        }

    }


}
