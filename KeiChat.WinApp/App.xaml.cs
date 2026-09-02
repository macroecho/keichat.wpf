/*
 * =================================================================
 * Copyright (c) 2023 KeiSoft, All Rights Reserved.
 * Author: macro
 * Date: 2023-08-29
 * Version: 6.0.0
 * 
 * 开源代码使用条款
 *     感谢您使用 Keisoft.Chat（以下简称“本代码”）。为确保合法、合规地使用本代码，请您仔细阅读以下条款。
 *     使用本代码即表示您同意遵守以下所有条款及适用法律法规。
 *     
 * 一、许可证信息
 *     本代码受 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。 
 *     
 * 二、合法使用限制（附加条件）
 *     您确认并同意：
 *     1. 仅将本代码用于合法目的；
 *     2. 遵守所有适用的法律法规，包括但不限于您所在司法管辖区、
 *        行为发生地及目标影响地的相关法律；
 *     3. 不得利用本代码从事以下行为：
 *        - 未授权访问计算机系统或网络
 *        - 侵犯他人知识产权、隐私权等合法权益
 *        - 危害网络安全或违反网络安全相关法律
 *        - 其他任何违反法律法规的行为
 *
 * 三、违反后果
 *     若您违反上述第二条中的任何限制，本代码授予您的使用授权立即终止。
 *     您必须立即停止使用本代码，并删除您持有的所有副本。
 *     因您违反本声明导致的任何法律责任由您自行承担，
 *     本项目维护者保留追究法律责任的权利。
 *
 * 四、免责声明  
 *     1. 本代码按“原样”提供，我们不对本代码的准确性、完整性、适用性、安全性作任何明示或暗示的担保，包括但不限于适销性、特定用途适用性的担保。
 *     2. 因使用本代码或衍生作品而产生的任何直接、间接、偶然、特殊或后果性损害（包括但不限于数据丢失、业务中断、利润损失等），我们不承担任何责任。
 * 
 * =================================================================
*/

using System.Windows;
using System.Security.Authentication;

using KeiChat.WinUI;
using KeiChat.WinUI.Views;

namespace KeiChat.WinApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string Tag = nameof(Tag);

        static App()
        {
            // KeiChat 业务服务的网关地址。
            AppSettings.SetGateway("https://demo-chat.example.com");

            // KeiChat 文件服务的网关地址。
            AppSettings.SetUploadFileGateway("https://demo-chat.example.com/upload-file");
            AppSettings.SetUploadVideoGateway("https://demo-chat.example.com/upload-video");
            AppSettings.SetUploadPictureGateway("https://demo-chat.example.com/upload-image");

            // 保留 upload-audio、upload-merge。
            AppSettings.SetUploadAudioGateway("https://demo-chat.example.com/upload-audio");
            AppSettings.SetUploadMergeGateway("https://demo-chat.example.com/upload-merge");

            // 即时通讯服务的网关地址。可写 https (ims)、或 http (im)
            AppSettings.SetIMServerHost("ims://demo-im.example.com");
            // 非 80 或 433 在这里指定端口。
            // AppSettings.SetIMServerPort(8080);

            // 即时通讯、文件独立进程
            // 本把 KeiChat.WinConApp.exe 复制到 KeiChat.WinApp 目录下。
            AppSettings.SetIMProcessProgramName("KeiChat.WinConApp.exe");
            AppSettings.SetFileProcessProgramName("KeiChat.WinConApp.exe");

            // 图片、视频查看器独立进程。视频播放组件，需要去 LibVL 官方下载，复制到 libvlc/win-x64 目录下
            AppSettings.SetMediaProcessProgramName("KeiChatDemo.exe");
        }

        public App()
        {
            // 直接获取版本号字符串
            string? versionString = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();

            if (versionString != null)
            {
                AppSettings.SetVersion(versionString);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. 处理 UI 线程未处理异常
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            // 2. 处理非 UI 线程未处理异常
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // 3. 处理未观察的 Task 异常
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // 如果有一个参数，可以查看图片或视频文件。
            if (e.Args != null && e.Args.Length == 1)
            {
                // 预加载。
                if ("-preload".Equals(e.Args[0]))
                {
                    LogUtilityEx.Info("KeiChatPhotoApplication", "-preload");
                    PhotoUI.KeiChatPhotoApplication.Preload();
                    return;
                }

                Resources.MergedDictionaries.Add(new PhotoUI.Themes.Theme());
                // 图片或视频查看程序
                PhotoUI.KeiChatPhotoApplication.Run(this, e.Args);
            }
            else if (e.Args == null || e.Args.Length < PhotoUI.KeiChatPhotoApplication.CmdArgsLength)
            {
                Resources.MergedDictionaries.Add(new WinUI.Themes.Theme());

                AppSettings.EnablePhoneLogin(true);
                AppSettings.SetViewUserAgreement(UserAgreementWindow.Open);
                AppSettings.SetViewPrivacyPolicy(PrivacyPolicyWindow.Open);

                // 设置自定义关于页面、首页右侧默认页。
                CustomizePageConfig.SettingAboutUC = new Views.UserControls.Setting.AboutUC();
                CustomizePageConfig.HoemRightCoverUC = new Views.UserControls.Home.RightCoverDefault();

                // 运行主程序。
                KeiChatApplication.Run(e);
            }
            // 图片或视频查看程序
            else
            {
                Resources.MergedDictionaries.Add(new PhotoUI.Themes.Theme());

                // 设置获取域名公证书服务地址。
                PhotoUI.AppSettings.SetCertificateUrl(Service.CertificateService.GenerateUrl());
                // 图片或视频查看程序
                PhotoUI.KeiChatPhotoApplication.Run(this, e.Args);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 记录日志。
            LogUtilityEx.Fatal(Tag, "UI 线程发生异常", e.Exception);
            // 阻止应用程序崩溃
            e.Handled = true;

            if (e.Exception is AuthenticationException authenticationException)
            {
                EntryWindow.ExcUnAuthorized();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                // 记录日志。
                LogUtilityEx.Fatal(Tag, "非 UI 线程发生异常", ex);
            }
            else
            {
                LogUtilityEx.Fatal(Tag, $"非 UI 线程发生异常，{e}");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // 记录日志。
            LogUtilityEx.Fatal(Tag, "Task 异常", e.Exception);
            // 标记异常为已观察，防止进程终止
            e.SetObserved();
        }
    }

}
