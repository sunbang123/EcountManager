using System.Collections.ObjectModel;
using System.Windows.Input;
using EcountManager.Models;
using EcountManager.Services;

namespace EcountManager.ViewModels;

public sealed class MemoWidgetViewModel : ViewModelBase
{
    private readonly IConsultationMemoStore _store;
    private ConsultationMemo? _selectedMemo;
    private string _businessName = string.Empty;
    private string _phoneNumber = string.Empty;
    private string _inquiry = string.Empty;
    private string _statusMessage = "상담 내용을 빠르게 기록하세요.";

    public MemoWidgetViewModel(IConsultationMemoStore store)
    {
        _store = store;
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        NewCommand = new RelayCommand(ClearEditor);
        _ = LoadAsync();
    }

    public ObservableCollection<ConsultationMemo> Memos { get; } = [];
    public ICommand SaveCommand { get; }
    public ICommand NewCommand { get; }

    public ConsultationMemo? SelectedMemo
    {
        get => _selectedMemo;
        set
        {
            if (!SetProperty(ref _selectedMemo, value) || value is null) return;
            BusinessName = value.BusinessName;
            PhoneNumber = value.PhoneNumber;
            Inquiry = value.Inquiry;
            StatusMessage = $"{value.CreatedAt:yyyy-MM-dd HH:mm} 생성 메모 편집 중";
        }
    }

    public string BusinessName
    {
        get => _businessName;
        set { if (SetProperty(ref _businessName, value)) RaiseSaveState(); }
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string Inquiry
    {
        get => _inquiry;
        set { if (SetProperty(ref _inquiry, value)) RaiseSaveState(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(BusinessName) && !string.IsNullOrWhiteSpace(Inquiry);

    private async Task LoadAsync()
    {
        try
        {
            var memos = await _store.LoadAsync();
            ReplaceSorted(memos);
        }
        catch (Exception ex)
        {
            StatusMessage = $"메모 불러오기 실패: {ex.Message}";
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            var memo = SelectedMemo is null
                ? new ConsultationMemo(Guid.NewGuid(), BusinessName.Trim(), PhoneNumber.Trim(), Inquiry.Trim(), DateTimeOffset.Now)
                : SelectedMemo with
                {
                    BusinessName = BusinessName.Trim(),
                    PhoneNumber = PhoneNumber.Trim(),
                    Inquiry = Inquiry.Trim()
                };

            var allMemos = Memos.Where(item => item.Id != memo.Id).Append(memo).ToArray();
            await _store.SaveAsync(allMemos);
            ReplaceSorted(allMemos);
            StatusMessage = SelectedMemo is null ? "상담 메모를 저장했습니다." : "상담 메모를 수정했습니다. 생성시간은 유지됩니다.";
            ClearEditor(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"메모 저장 실패: {ex.Message}";
        }
    }

    private void ClearEditor() => ClearEditor(true);

    private void ClearEditor(bool updateStatus)
    {
        _selectedMemo = null;
        OnPropertyChanged(nameof(SelectedMemo));
        BusinessName = string.Empty;
        PhoneNumber = string.Empty;
        Inquiry = string.Empty;
        if (updateStatus) StatusMessage = "새 상담 메모를 작성합니다.";
    }

    private void ReplaceSorted(IEnumerable<ConsultationMemo> memos)
    {
        Memos.Clear();
        foreach (var memo in memos.OrderByDescending(item => item.CreatedAt)) Memos.Add(memo);
    }

    private void RaiseSaveState() => ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
}
