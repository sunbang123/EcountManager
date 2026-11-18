using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EcountManager.Services;
using EcountManager.ViewModels;

namespace EcountManager;

public partial class MainWindow : Window
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public MainWindow()
    {
        InitializeComponent();
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("EcountManager/0.2");
        DataContext = new MainWindowViewModel(new EcountApiClient(HttpClient));
    }

    private void ApiKeyBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ApiCertificateKey = passwordBox.Password;
        }
    }

    private void WindowHeader_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

}
