using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace SafeDeployStudio.Desktop;

public partial class MainWindow : Window
{
    private const string DefaultEndpoint = "https://localhost:7199";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SafeDeployStudio", "WebView2Profile"));

            await MainWebView.EnsureCoreWebView2Async(env);
            MainWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            MainWebView.Source = new Uri(DefaultEndpoint);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to initialize WebView2 runtime: {ex.Message}\n\nPlease ensure Microsoft Edge WebView2 Evergreen Runtime is installed.",
                "SafeDeploy Studio", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MenuNewDeployment_Click(object sender, RoutedEventArgs e)
    {
        MainWebView?.CoreWebView2?.Navigate($"{DefaultEndpoint}/deploy");
    }

    private void MenuRefresh_Click(object sender, RoutedEventArgs e)
    {
        MainWebView?.CoreWebView2?.Reload();
    }

    private void MenuDevTools_Click(object sender, RoutedEventArgs e)
    {
        MainWebView?.CoreWebView2?.OpenDevToolsWindow();
    }

    private void MenuOpenVault_Click(object sender, RoutedEventArgs e)
    {
        var vaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SafeDeployStudio", "Vault");

        Directory.CreateDirectory(vaultPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = vaultPath,
            UseShellExecute = true
        });
    }

    private void MenuVerifyConnections_Click(object sender, RoutedEventArgs e)
    {
        MainWebView?.CoreWebView2?.Navigate($"{DefaultEndpoint}/projects");
    }

    private void MenuDocs_Click(object sender, RoutedEventArgs e)
    {
        MainWebView?.CoreWebView2?.Navigate("https://vishal-07-prajapati.github.io/safedeploy-studio-site");
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "SafeDeploy Studio — Enterprise Database Deployment & Management Platform\n\n" +
            "Version: 1.0.0 LTS\n" +
            "Runtime: .NET 10.0\n" +
            "Architecture: Clean Architecture Modular Monolith\n" +
            "AST Parser: Microsoft.SqlServer.TransactSql.ScriptDom 180\n" +
            "Diff Engine: DiffPlex 1.9\n" +
            "Provider: Microsoft SQL Server, Azure SQL\n\n" +
            "© 2026 SafeDeploy Studio Open Source Project. MIT License.",
            "About SafeDeploy Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
