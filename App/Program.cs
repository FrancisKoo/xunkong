﻿namespace Xunkong.Desktop;

public static class Program
{
    [global::System.Runtime.InteropServices.DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler", " 1.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.STAThreadAttribute]
    static void Main(string[] args)
    {
        // 不要使用 async Task Main，否则 WebView2 会出错

        Environment.CurrentDirectory = AppContext.BaseDirectory;

        if (args.FirstOrDefault() == "DoNotClickToast")
        {
            return;
        }

        if (args.FirstOrDefault() == "StartGame")
        {
            if (InvokeService.StartGameAsync().GetAwaiter().GetResult())
            {
                InvokeService.CheckTransformerReachedAndHomeCoinFullAsync().GetAwaiter().GetResult();
            }
            return;
        }

        if (args.FirstOrDefault() == "DailyCheckIn")
        {
            InvokeService.SignInAllAccountAsync().GetAwaiter().GetResult();
            return;
        }

        if (args.FirstOrDefault() == "RefreshTile")
        {
            InvokeService.RefreshDailyNoteTilesAsync().GetAwaiter().GetResult();
            return;
        }

        if (args.FirstOrDefault() == "dailynote")
        {
            InvokeService.RefreshDailyNoteTilesAsync().GetAwaiter().GetResult();
            return;
        }

        //if (args.FirstOrDefault() == "/InvokerPRAID:")
        //{
        //    if (args[2] == "DailyNoteTask")
        //    {
        //        InvokeService.RefreshDailyNoteTilesAsync().GetAwaiter().GetResult();
        //    }
        //    if (args[2] == "HoyolabCheckInTask")
        //    {
        //        InvokeService.SignInAllAccountAsync().GetAwaiter().GetResult();
        //    }
        //    return;
        //}

        XamlCheckProcessRequirements();

        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        global::Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });

        ServiceProvider.Dispose();
    }
}
