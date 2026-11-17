using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using EcountManager.Models;
using EcountManager.Services;

namespace EcountManager.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IEcountQueryService _queryService;
    private Customer? _selectedCustomer;
    private CustomerDashboard? _dashboard;
    private string _customerSearchText = string.Empty;
    private string _saleSearchText = string.Empty;
    private string _inventorySearchText = string.Empty;
    private string _statusMessage = "고객을 선택해 주세요.";
    private bool _isLoading;
    private int _selectedTabIndex;

    public MainWindowViewModel(IEcountQueryService queryService)
    {
        _queryService = queryService;
        CustomersView = CollectionViewSource.GetDefaultView(Customers);
        SalesView = CollectionViewSource.GetDefaultView(Sales);
        InventoryView = CollectionViewSource.GetDefaultView(Inventory);
        ReceivablesView = CollectionViewSource.GetDefaultView(Receivables);
        SalesView.Filter = FilterSale;
        InventoryView.Filter = FilterInventory;

        RefreshCommand = new RelayCommand(() => _ = RefreshAsync(), () => SelectedCustomer is not null && !IsLoading);
        OpenInventoryTabCommand = new RelayCommand(() => SelectedTabIndex = 1);
        _ = InitializeAsync();
    }

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<SaleRecord> Sales { get; } = [];
    public ObservableCollection<InventoryItem> Inventory { get; } = [];
    public ObservableCollection<ReceivableRecord> Receivables { get; } = [];
    public ICollectionView CustomersView { get; }
    public ICollectionView SalesView { get; }
    public ICollectionView InventoryView { get; }
    public ICollectionView ReceivablesView { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenInventoryTabCommand { get; }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value) && value is not null)
            {
                ((RelayCommand)RefreshCommand).RaiseCanExecuteChanged();
                _ = LoadDashboardAsync(value.Code);
            }
        }
    }

    public CustomerDashboard? Dashboard
    {
        get => _dashboard;
        private set => SetProperty(ref _dashboard, value);
    }

    public string CustomerSearchText
    {
        get => _customerSearchText;
        set
        {
            if (SetProperty(ref _customerSearchText, value)) _ = SearchCustomersAsync(value);
        }
    }

    public string SaleSearchText
    {
        get => _saleSearchText;
        set
        {
            if (SetProperty(ref _saleSearchText, value)) SalesView.Refresh();
        }
    }

    public string InventorySearchText
    {
        get => _inventorySearchText;
        set
        {
            if (SetProperty(ref _inventorySearchText, value)) InventoryView.Refresh();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value)) ((RelayCommand)RefreshCommand).RaiseCanExecuteChanged();
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    private async Task InitializeAsync()
    {
        await SearchCustomersAsync(null);
        SelectedCustomer = Customers.FirstOrDefault();
    }

    private async Task SearchCustomersAsync(string? searchText)
    {
        try
        {
            var selectedCode = SelectedCustomer?.Code;
            var results = await _queryService.SearchCustomersAsync(searchText);
            Customers.Clear();
            foreach (var customer in results) Customers.Add(customer);

            if (selectedCode is not null)
            {
                _selectedCustomer = Customers.FirstOrDefault(x => x.Code == selectedCode);
                OnPropertyChanged(nameof(SelectedCustomer));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"고객 검색 실패: {ex.Message}";
        }
    }

    private async Task RefreshAsync()
    {
        if (SelectedCustomer is not null) await LoadDashboardAsync(SelectedCustomer.Code);
    }

    private async Task LoadDashboardAsync(string customerCode)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Ecount 조회 데이터를 불러오는 중입니다...";
            var dashboard = await _queryService.GetCustomerDashboardAsync(customerCode);
            if (SelectedCustomer?.Code != customerCode) return;

            Dashboard = dashboard;
            ReplaceItems(Sales, dashboard.Sales);
            ReplaceItems(Inventory, dashboard.Inventory);
            ReplaceItems(Receivables, dashboard.Receivables);
            StatusMessage = $"{dashboard.Customer.Name} 조회 완료 · {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"데이터 조회 실패: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool FilterSale(object item)
    {
        if (item is not SaleRecord sale || string.IsNullOrWhiteSpace(SaleSearchText)) return true;
        var term = SaleSearchText.Trim();
        return sale.ItemName.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || sale.ItemCode.Contains(term, StringComparison.OrdinalIgnoreCase)
            || sale.Specification.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || sale.EnteredBy.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || sale.SlipNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
            || sale.TotalAmount.ToString("N0").Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterInventory(object item)
    {
        if (item is not InventoryItem inventoryItem || string.IsNullOrWhiteSpace(InventorySearchText)) return true;
        var term = InventorySearchText.Trim();
        return inventoryItem.ItemName.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || inventoryItem.ItemCode.Contains(term, StringComparison.OrdinalIgnoreCase)
            || inventoryItem.Specification.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || inventoryItem.StandardPrice.ToString("N0").Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static void ReplaceItems<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items) collection.Add(item);
    }
}
