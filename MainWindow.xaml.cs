using System;
using System.IO;
using System.Windows;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.Web.WebView2.Core;


namespace DogPrincess;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                MessageBox.Show(
                    "DogPrincess requires Windows 10 October 2018 Update (build 17763) or later.",
                    "Unsupported OS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown();
                return;
            }

            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DogPrincess", "UserData");

            Directory.CreateDirectory(userDataFolder);

            var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
            var environment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: userDataFolder,
                options: options);

            await WebView.EnsureCoreWebView2Async(environment);

            WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            WebView.CoreWebView2.WebResourceRequested += HandleWebResourceRequested;

            WebView.Source = new Uri("https://app.local/index.html");
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Initialization failed:\n{exception}", "DogPrincess");
            Application.Current.Shutdown();
        }
    }

    private void HandleWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return;

        var uri = new Uri(e.Request.Uri);
        if (uri.Host != "app.local") return;

        string relativePath = uri.AbsolutePath.TrimStart('/');
        string resourceName = $"DogPrincess.wwwroot.{relativePath.Replace('/', '.')}";

        var asm = Assembly.GetExecutingAssembly();
        Stream? stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null)
            return;

        string contentType = GetContentType(relativePath);

        var response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
            stream, 200, "OK", $"Content-Type: {contentType}");

        e.Response = response;
    }

    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html",
        [".js"] = "application/javascript",
        [".ico"] = "image/x-icon",
        [".swf"] = "application/x-shockwave-flash",
        [".wasm"] = "application/wasm",
        [".xml"] = "application/xml",
        [".json"] = "application/json",
        [".txt"] = "text/plain"
    };

    private static string GetContentType(string path)
    {
        string extension = Path.GetExtension(path);
        return (!string.IsNullOrEmpty(extension) && MimeTypes.TryGetValue(extension, out var mime))
            ? mime
            : "application/octet-stream";
    }
}