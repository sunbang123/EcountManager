using System.Windows;
using System.Windows.Controls;
using EcountManager.Models;

namespace EcountManager;

public partial class SnippetFillWindow : Window
{
    private readonly CopySnippet _snippet;
    private readonly string[] _parts;
    private readonly List<TextBox> _inputs = [];

    public SnippetFillWindow(CopySnippet snippet)
    {
        InitializeComponent();
        _snippet = snippet;
        _parts = snippet.Template.Split(["``"], StringSplitOptions.None);
        TitleText.Text = snippet.Title;
        TemplateText.Text = snippet.Template.Replace("``", " ＿＿＿ ");

        for (var index = 0; index < _parts.Length - 1; index++)
        {
            FieldsPanel.Children.Add(new TextBlock
            {
                Text = $"빈칸 {index + 1}",
                Foreground = System.Windows.Media.Brushes.DimGray,
                FontSize = 11,
                Margin = new Thickness(1, index == 0 ? 0 : 10, 0, 4)
            });

            var input = new TextBox { MinHeight = 36 };
            _inputs.Add(input);
            FieldsPanel.Children.Add(input);
        }

        Loaded += (_, _) => _inputs.FirstOrDefault()?.Focus();
    }

    public string CompletedText { get; private set; } = string.Empty;

    private void Copy_OnClick(object sender, RoutedEventArgs e)
    {
        var result = _parts[0];
        for (var index = 0; index < _inputs.Count; index++)
            result += _inputs[index].Text.Trim() + _parts[index + 1];

        try
        {
            Clipboard.SetText(result);
            CompletedText = result;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"클립보드 복사에 실패했습니다.\n{ex.Message}", "복사 실패", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
