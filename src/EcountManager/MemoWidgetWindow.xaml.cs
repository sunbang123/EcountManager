using System.Windows;
using System.Windows.Input;
using EcountManager.Models;
using EcountManager.Services;
using EcountManager.ViewModels;

namespace EcountManager;

public partial class MemoWidgetWindow : Window
{
    private readonly MemoWidgetViewModel _viewModel;

    public MemoWidgetWindow()
    {
        InitializeComponent();
        _viewModel = new MemoWidgetViewModel(
            new JsonConsultationMemoStore(),
            new JsonLocalListStore<CopySnippet>("copy-snippets.json"),
            new JsonLocalListStore<WorkMemo>("work-memos.json"));
        _viewModel.SnippetCopyRequested += OnSnippetCopyRequested;
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel.SnippetCopyRequested -= OnSnippetCopyRequested;
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void OnSnippetCopyRequested(CopySnippet snippet)
    {
        try
        {
            if (!snippet.Template.Contains("``", StringComparison.Ordinal))
            {
                Clipboard.SetText(snippet.Template);
                _viewModel.NotifySnippetCopied(snippet.Title);
                return;
            }

            var dialog = new SnippetFillWindow(snippet) { Owner = this };
            if (dialog.ShowDialog() == true) _viewModel.NotifySnippetCopied(snippet.Title);
        }
        catch (Exception ex)
        {
            _viewModel.NotifySnippetCopyFailed(ex.Message);
        }
    }
}

