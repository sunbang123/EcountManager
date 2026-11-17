using System.Windows;
using System.Windows.Input;
using EcountManager.Services;
using EcountManager.ViewModels;

namespace EcountManager;

public partial class MemoWidgetWindow : Window
{
    public MemoWidgetWindow()
    {
        InitializeComponent();
        DataContext = new MemoWidgetViewModel(new JsonConsultationMemoStore());
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Hide_OnClick(object sender, RoutedEventArgs e) => Hide();

    private void Close_OnClick(object sender, RoutedEventArgs e) => Close();
}

