using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EcountManager.Models;
using EcountManager.Services;

namespace EcountManager.ViewModels;

public sealed class MemoWidgetViewModel : ViewModelBase
{
    private readonly IConsultationMemoStore _consultationStore;
    private readonly ILocalListStore<CopySnippet> _snippetStore;
    private readonly ILocalListStore<WorkMemo> _workMemoStore;
    private ConsultationMemo? _selectedMemo;
    private string _businessName = string.Empty;
    private string _phoneNumber = string.Empty;
    private string _businessRegistrationNumber = string.Empty;
    private string _inquiry = string.Empty;
    private string _snippetTitle = string.Empty;
    private string _snippetTemplate = string.Empty;
    private string _workMemoText = string.Empty;
    private string _statusMessage = "세 가지 메모 친구가 준비됐어요.";

    public MemoWidgetViewModel(
        IConsultationMemoStore consultationStore,
        ILocalListStore<CopySnippet> snippetStore,
        ILocalListStore<WorkMemo> workMemoStore)
    {
        _consultationStore = consultationStore;
        _snippetStore = snippetStore;
        _workMemoStore = workMemoStore;

        SaveMemoCommand = new AsyncRelayCommand(SaveMemoAsync, CanSaveMemo);
        NewMemoCommand = new RelayCommand(ClearMemoEditor);
        CopyReportCommand = new RelayCommand(CopyReport, CanCopyReport);
        CopyBusinessNameCommand = new RelayCommand(
            () => CopySingleValue("상호명", BusinessName),
            () => !string.IsNullOrWhiteSpace(BusinessName));
        CopyBusinessRegistrationNumberCommand = new RelayCommand(
            () => CopySingleValue("사업자등록번호", BusinessRegistrationNumber),
            () => !string.IsNullOrWhiteSpace(BusinessRegistrationNumber));
        DeleteSelectedMemoCommand = new AsyncRelayCommand(DeleteSelectedMemoAsync, () => SelectedMemo is not null);
        ClearAllMemosCommand = new AsyncRelayCommand(ClearAllMemosAsync, () => Memos.Count > 0);

        SaveSnippetCommand = new AsyncRelayCommand(SaveSnippetAsync, CanSaveSnippet);
        CopySnippetCommand = new RelayCommand<CopySnippet>(snippet => SnippetCopyRequested?.Invoke(snippet));
        DeleteSnippetCommand = new AsyncRelayCommand<CopySnippet>(DeleteSnippetAsync);

        SaveWorkMemoCommand = new AsyncRelayCommand(SaveWorkMemoAsync, () => !string.IsNullOrWhiteSpace(WorkMemoText));
        CompleteWorkMemoCommand = new AsyncRelayCommand<WorkMemo>(CompleteWorkMemoAsync);

        _ = LoadAsync();
    }

    public event Action<CopySnippet>? SnippetCopyRequested;

    public ObservableCollection<ConsultationMemo> Memos { get; } = [];
    public ObservableCollection<CopySnippet> Snippets { get; } = [];
    public ObservableCollection<WorkMemo> WorkMemos { get; } = [];

    public ICommand SaveMemoCommand { get; }
    public ICommand NewMemoCommand { get; }
    public ICommand CopyReportCommand { get; }
    public ICommand CopyBusinessNameCommand { get; }
    public ICommand CopyBusinessRegistrationNumberCommand { get; }
    public ICommand DeleteSelectedMemoCommand { get; }
    public ICommand ClearAllMemosCommand { get; }
    public ICommand SaveSnippetCommand { get; }
    public ICommand CopySnippetCommand { get; }
    public ICommand DeleteSnippetCommand { get; }
    public ICommand SaveWorkMemoCommand { get; }
    public ICommand CompleteWorkMemoCommand { get; }

    public ConsultationMemo? SelectedMemo
    {
        get => _selectedMemo;
        set
        {
            if (!SetProperty(ref _selectedMemo, value)) return;
            ((AsyncRelayCommand)DeleteSelectedMemoCommand).RaiseCanExecuteChanged();
            if (value is null) return;
            BusinessName = value.BusinessName;
            PhoneNumber = value.PhoneNumber;
            BusinessRegistrationNumber = value.BusinessRegistrationNumber ?? string.Empty;
            Inquiry = value.Inquiry;
            StatusMessage = $"{value.CreatedAt:yyyy-MM-dd HH:mm} 생성 메모 편집 중";
        }
    }

    public string BusinessName
    {
        get => _businessName;
        set
        {
            if (!SetProperty(ref _businessName, value)) return;
            RaiseMemoCommandStates();
        }
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (!SetProperty(ref _phoneNumber, value)) return;
            ((RelayCommand)CopyReportCommand).RaiseCanExecuteChanged();
        }
    }

    public string BusinessRegistrationNumber
    {
        get => _businessRegistrationNumber;
        set
        {
            if (!SetProperty(ref _businessRegistrationNumber, value)) return;
            ((RelayCommand)CopyBusinessRegistrationNumberCommand).RaiseCanExecuteChanged();
        }
    }

    public string Inquiry
    {
        get => _inquiry;
        set
        {
            if (!SetProperty(ref _inquiry, value)) return;
            RaiseMemoCommandStates();
        }
    }

    public string SnippetTitle
    {
        get => _snippetTitle;
        set
        {
            if (SetProperty(ref _snippetTitle, value)) RaiseSnippetSaveState();
        }
    }

    public string SnippetTemplate
    {
        get => _snippetTemplate;
        set
        {
            if (SetProperty(ref _snippetTemplate, value)) RaiseSnippetSaveState();
        }
    }

    public string WorkMemoText
    {
        get => _workMemoText;
        set
        {
            if (SetProperty(ref _workMemoText, value))
                ((AsyncRelayCommand)SaveWorkMemoCommand).RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void NotifySnippetCopied(string title) => StatusMessage = $"‘{title}’ 문장을 클립보드에 복사했습니다.";

    public void NotifySnippetCopyFailed(string message) => StatusMessage = $"문장 복사 실패: {message}";

    private bool CanSaveMemo() => !string.IsNullOrWhiteSpace(BusinessName) && !string.IsNullOrWhiteSpace(Inquiry);

    private bool CanCopyReport() => !string.IsNullOrWhiteSpace(BusinessName) || !string.IsNullOrWhiteSpace(Inquiry);

    private bool CanSaveSnippet() => !string.IsNullOrWhiteSpace(SnippetTitle) && !string.IsNullOrWhiteSpace(SnippetTemplate);

    private async Task LoadAsync()
    {
        try
        {
            var consultationTask = _consultationStore.LoadAsync();
            var snippetTask = _snippetStore.LoadAsync();
            var workMemoTask = _workMemoStore.LoadAsync();
            await Task.WhenAll(consultationTask, snippetTask, workMemoTask);

            ReplaceSorted(Memos, consultationTask.Result, item => item.CreatedAt);
            ReplaceSorted(Snippets, snippetTask.Result, item => item.CreatedAt);
            ReplaceSorted(WorkMemos, workMemoTask.Result, item => item.CreatedAt);
            RaiseCollectionCommandStates();
        }
        catch (Exception ex)
        {
            StatusMessage = $"메모 불러오기 실패: {ex.Message}";
        }
    }

    private async Task SaveMemoAsync()
    {
        try
        {
            var isNew = SelectedMemo is null;
            var memo = isNew
                ? new ConsultationMemo(Guid.NewGuid(), BusinessName.Trim(), PhoneNumber.Trim(), Inquiry.Trim(), DateTimeOffset.Now)
                {
                    BusinessRegistrationNumber = BusinessRegistrationNumber.Trim()
                }
                : SelectedMemo! with
                {
                    BusinessName = BusinessName.Trim(),
                    PhoneNumber = PhoneNumber.Trim(),
                    BusinessRegistrationNumber = BusinessRegistrationNumber.Trim(),
                    Inquiry = Inquiry.Trim()
                };

            var allMemos = Memos.Where(item => item.Id != memo.Id).Append(memo).ToArray();
            await _consultationStore.SaveAsync(allMemos);
            ReplaceSorted(Memos, allMemos, item => item.CreatedAt);
            StatusMessage = isNew ? "상담 메모를 저장했습니다." : "상담 메모를 수정했습니다. 생성시간은 유지됩니다.";
            ClearMemoEditor(false);
            RaiseCollectionCommandStates();
        }
        catch (Exception ex)
        {
            StatusMessage = $"메모 저장 실패: {ex.Message}";
        }
    }

    private void CopyReport()
    {
        var registrationLine = string.IsNullOrWhiteSpace(BusinessRegistrationNumber)
            ? string.Empty
            : $"\r\n사업자등록번호: {BusinessRegistrationNumber.Trim()}";
        var report = $"{BusinessName.Trim()} / {PhoneNumber.Trim()}{registrationLine}\r\n\r\n문의내용: {Inquiry.Trim()}";
        try
        {
            Clipboard.SetText(report);
            StatusMessage = "보고용 상담 양식을 클립보드에 복사했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"클립보드 복사 실패: {ex.Message}";
        }
    }

    private void CopySingleValue(string label, string value)
    {
        try
        {
            Clipboard.SetText(value.Trim());
            StatusMessage = $"{label}을(를) 클립보드에 복사했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"클립보드 복사 실패: {ex.Message}";
        }
    }

    private async Task DeleteSelectedMemoAsync()
    {
        if (SelectedMemo is null) return;
        var answer = MessageBox.Show(
            $"‘{SelectedMemo.BusinessName}’ 상담 메모를 삭제할까요?",
            "상담 메모 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        var remaining = Memos.Where(item => item.Id != SelectedMemo.Id).ToArray();
        await _consultationStore.SaveAsync(remaining);
        ReplaceSorted(Memos, remaining, item => item.CreatedAt);
        ClearMemoEditor(false);
        StatusMessage = "선택한 상담 메모를 삭제했습니다.";
        RaiseCollectionCommandStates();
    }

    private async Task ClearAllMemosAsync()
    {
        var answer = MessageBox.Show(
            "오늘의 상담 메모를 모두 삭제할까요? 이 작업은 되돌릴 수 없습니다.",
            "상담 메모 전체 초기화",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        await _consultationStore.SaveAsync([]);
        Memos.Clear();
        ClearMemoEditor(false);
        StatusMessage = "상담 메모를 모두 초기화했습니다.";
        RaiseCollectionCommandStates();
    }

    private async Task SaveSnippetAsync()
    {
        try
        {
            var snippet = new CopySnippet(Guid.NewGuid(), SnippetTitle.Trim(), SnippetTemplate.Trim(), DateTimeOffset.Now);
            var allSnippets = Snippets.Append(snippet).ToArray();
            await _snippetStore.SaveAsync(allSnippets);
            ReplaceSorted(Snippets, allSnippets, item => item.CreatedAt);
            var placeholderCount = CountPlaceholders(snippet.Template);
            SnippetTitle = string.Empty;
            SnippetTemplate = string.Empty;
            StatusMessage = placeholderCount == 0
                ? "복붙 문장을 등록했습니다."
                : $"복붙 문장을 등록했습니다. 입력칸 {placeholderCount}개가 생성됩니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"복붙 문장 저장 실패: {ex.Message}";
        }
    }

    private async Task DeleteSnippetAsync(CopySnippet snippet)
    {
        try
        {
            var remaining = Snippets.Where(item => item.Id != snippet.Id).ToArray();
            await _snippetStore.SaveAsync(remaining);
            ReplaceSorted(Snippets, remaining, item => item.CreatedAt);
            StatusMessage = $"‘{snippet.Title}’ 문장을 삭제했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"복붙 문장 삭제 실패: {ex.Message}";
        }
    }

    private async Task SaveWorkMemoAsync()
    {
        try
        {
            var memo = new WorkMemo(Guid.NewGuid(), WorkMemoText.Trim(), DateTimeOffset.Now);
            var allMemos = WorkMemos.Append(memo).ToArray();
            await _workMemoStore.SaveAsync(allMemos);
            ReplaceSorted(WorkMemos, allMemos, item => item.CreatedAt);
            WorkMemoText = string.Empty;
            StatusMessage = "진행 중인 업무를 기억해둘게요.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"업무 메모 저장 실패: {ex.Message}";
        }
    }

    private async Task CompleteWorkMemoAsync(WorkMemo memo)
    {
        try
        {
            var remaining = WorkMemos.Where(item => item.Id != memo.Id).ToArray();
            await _workMemoStore.SaveAsync(remaining);
            ReplaceSorted(WorkMemos, remaining, item => item.CreatedAt);
            StatusMessage = $"완료: {memo.Content}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"업무 완료 처리 실패: {ex.Message}";
        }
    }

    private void ClearMemoEditor() => ClearMemoEditor(true);

    private void ClearMemoEditor(bool updateStatus)
    {
        _selectedMemo = null;
        OnPropertyChanged(nameof(SelectedMemo));
        BusinessName = string.Empty;
        PhoneNumber = string.Empty;
        BusinessRegistrationNumber = string.Empty;
        Inquiry = string.Empty;
        ((AsyncRelayCommand)DeleteSelectedMemoCommand).RaiseCanExecuteChanged();
        if (updateStatus) StatusMessage = "새 상담 메모를 작성합니다.";
    }

    private void RaiseMemoCommandStates()
    {
        ((AsyncRelayCommand)SaveMemoCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CopyReportCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CopyBusinessNameCommand).RaiseCanExecuteChanged();
    }

    private void RaiseSnippetSaveState() => ((AsyncRelayCommand)SaveSnippetCommand).RaiseCanExecuteChanged();

    private void RaiseCollectionCommandStates()
    {
        ((AsyncRelayCommand)ClearAllMemosCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)DeleteSelectedMemoCommand).RaiseCanExecuteChanged();
    }

    private static int CountPlaceholders(string template) => template.Split("``").Length - 1;

    private static void ReplaceSorted<T>(ObservableCollection<T> target, IEnumerable<T> items, Func<T, DateTimeOffset> createdAt)
    {
        target.Clear();
        foreach (var item in items.OrderByDescending(createdAt)) target.Add(item);
    }
}
