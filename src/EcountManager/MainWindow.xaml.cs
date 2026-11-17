using System.Windows;
using EcountManager.Services;
using EcountManager.ViewModels;

namespace EcountManager;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(new MockEcountQueryService());
    }
}
