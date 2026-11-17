using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using EcountManager.Services;
using EcountManager.ViewModels;

namespace EcountManager;

public partial class MainWindow : Window
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private MemoWidgetWindow? _memoWidget;

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

    private void OpenMemoWidget_OnClick(object sender, RoutedEventArgs e)
    {
        if (_memoWidget is null)
        {
            _memoWidget = new MemoWidgetWindow();
            _memoWidget.Closed += (_, _) => _memoWidget = null;
            _memoWidget.Show();
        }
        else
        {
            _memoWidget.Show();
            _memoWidget.Activate();
        }
    }
}
