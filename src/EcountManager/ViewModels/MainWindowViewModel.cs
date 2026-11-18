using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using EcountManager.Models;
using EcountManager.Services;

namespace EcountManager.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const string AllGroups = "전체 그룹";
    private readonly IEcountApiClient _apiClient;
    private EcountSession? _session;
    private string _companyCode = string.Empty;
    private string _userId = string.Empty;
    private string _apiCertificateKey = string.Empty;
    private string _searchText = string.Empty;
    private string _connectionStatus = "연결 정보를 입력해 주세요.";
    private string _zone = "-";
    private bool _useTestServer = true;
    private bool _isBusy;
    private DateTimeOffset? _lastSyncedAt;
    private string? _lastSearchCode;
    private string _selectedGroup = AllGroups;

    public MainWindowViewModel(IEcountApiClient apiClient)
    {
        _apiClient = apiClient;
        ProductsView = CollectionViewSource.GetDefaultView(Products);
        WarehouseStocksView = CollectionViewSource.GetDefaultView(WarehouseStocks);
        ProductsView.Filter = FilterProduct;
        WarehouseStocksView.Filter = FilterWarehouseStock;
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        SearchCommand = new AsyncRelayCommand(SearchAsync, CanSearch);
        RefreshCommand = new AsyncRelayCommand(RefreshLastSearchAsync, CanRefresh);
    }

    public ObservableCollection<EcountProduct> Products { get; } = [];
    public ObservableCollection<EcountWarehouseStock> WarehouseStocks { get; } = [];
    public ObservableCollection<string> GroupOptions { get; } = [AllGroups];
    public ICollectionView ProductsView { get; }
    public ICollectionView WarehouseStocksView { get; }
    public ICommand ConnectCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand RefreshCommand { get; }

    public string CompanyCode
    {
        get => _companyCode;
        set { if (SetProperty(ref _companyCode, value)) RaiseCommandStates(); }
    }

    public string UserId
    {
        get => _userId;
        set { if (SetProperty(ref _userId, value)) RaiseCommandStates(); }
    }

    public string ApiCertificateKey
    {
        get => _apiCertificateKey;
        set { if (SetProperty(ref _apiCertificateKey, value)) RaiseCommandStates(); }
    }

    public bool UseTestServer
    {
        get => _useTestServer;
        set => SetProperty(ref _useTestServer, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value)) RaiseCommandStates();
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    public string SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value)) ProductsView.Refresh();
        }
    }

    public string Zone
    {
        get => _zone;
        private set => SetProperty(ref _zone, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value)) RaiseCommandStates();
        }
    }

    public DateTimeOffset? LastSyncedAt
    {
        get => _lastSyncedAt;
        private set => SetProperty(ref _lastSyncedAt, value);
    }

    public int ProductCount => Products.Count;
    public int WarehouseStockCount => WarehouseStocks.Count;
    public decimal TotalStock => Products.Sum(product => product.CurrentStock);
    public string ServerMode => UseTestServer ? "테스트 서버" : "운영 서버";

    private bool CanConnect() =>
        !IsBusy &&
        !string.IsNullOrWhiteSpace(CompanyCode) &&
        !string.IsNullOrWhiteSpace(UserId) &&
        !string.IsNullOrWhiteSpace(ApiCertificateKey);

    private bool CanSearch() => _session is not null && !IsBusy && !string.IsNullOrWhiteSpace(SearchText);

    private bool CanRefresh() => _session is not null && !IsBusy && !string.IsNullOrWhiteSpace(_lastSearchCode);

    private async Task ConnectAsync()
    {
        try
        {
            IsBusy = true;
            ConnectionStatus = "Zone 조회 및 로그인 중...";
            var settings = new EcountConnectionSettings(
                CompanyCode.Trim(),
                UserId.Trim(),
                ApiCertificateKey,
                UseTestServer);
            _session = await _apiClient.ConnectAsync(settings);
            Zone = _session.Zone;
            OnPropertyChanged(nameof(ServerMode));
            _lastSearchCode = null;
            ReplaceItems(Products, []);
            ReplaceItems(WarehouseStocks, []);
            ReplaceItems(GroupOptions, [AllGroups]);
            SelectedGroup = AllGroups;
            LastSyncedAt = null;
            NotifySummary();
            ConnectionStatus = "Ecount 연결 완료 · 정확한 품목코드를 입력해 조회하세요.";
        }
        catch (Exception ex)
        {
            _session = null;
            ConnectionStatus = $"연결 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            IsBusy = true;
            await SearchCoreAsync(SearchText.Trim());
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"품목 조회 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshLastSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastSearchCode)) return;
        try
        {
            IsBusy = true;
            await SearchCoreAsync(_lastSearchCode);
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"품목 재조회 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SearchCoreAsync(string productCode)
    {
        if (_session is null) return;
        var baseDate = DateOnly.FromDateTime(DateTime.Today);

        ConnectionStatus = $"{productCode} 품목 정보를 불러오는 중...";
        var products = await _apiClient.GetProductsAsync(_session, productCode);
        ConnectionStatus = $"{productCode} 재고 현황을 불러오는 중...";
        var inventory = await _apiClient.GetInventoryAsync(_session, baseDate, productCode);
        foreach (var product in products)
        {
            if (inventory.TryGetValue(product.Code, out var quantity)) product.CurrentStock = quantity;
        }

        ConnectionStatus = $"{productCode} 창고별 재고를 불러오는 중...";
        var warehouseStocks = await _apiClient.GetWarehouseInventoryAsync(_session, baseDate, productCode);
        UpsertProducts(products);
        UpsertWarehouseStocks(warehouseStocks, productCode);
        RefreshGroupOptions();
        _lastSearchCode = productCode;
        LastSyncedAt = DateTimeOffset.Now;
        NotifySummary();
        RaiseCommandStates();
        ConnectionStatus = products.Count == 0
            ? $"{productCode} 품목을 찾지 못했습니다. 정확한 품목코드를 확인하세요."
            : $"{productCode} 조회 완료 · API는 검색 버튼을 누를 때만 호출됩니다.";
    }

    private bool FilterProduct(object item)
    {
        if (item is not EcountProduct product || SelectedGroup == AllGroups) return true;
        return string.Equals(product.GroupDisplayName, SelectedGroup, StringComparison.CurrentCultureIgnoreCase);
    }

    private bool FilterWarehouseStock(object item)
    {
        return item is EcountWarehouseStock;
    }

    private void RaiseCommandStates()
    {
        ((AsyncRelayCommand)ConnectCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)SearchCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)RefreshCommand).RaiseCanExecuteChanged();
    }

    private void NotifySummary()
    {
        OnPropertyChanged(nameof(ProductCount));
        OnPropertyChanged(nameof(WarehouseStockCount));
        OnPropertyChanged(nameof(TotalStock));
    }

    private static void ReplaceItems<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items) collection.Add(item);
    }

    private void UpsertProducts(IEnumerable<EcountProduct> products)
    {
        foreach (var product in products)
        {
            var existing = Products.FirstOrDefault(item =>
                string.Equals(item.Code, product.Code, StringComparison.OrdinalIgnoreCase));
            if (existing is not null) Products.Remove(existing);
            Products.Add(product);
        }

        ProductsView.Refresh();
    }

    private void UpsertWarehouseStocks(IEnumerable<EcountWarehouseStock> warehouseStocks, string productCode)
    {
        foreach (var existing in WarehouseStocks
                     .Where(item => string.Equals(item.ProductCode, productCode, StringComparison.OrdinalIgnoreCase))
                     .ToArray())
        {
            WarehouseStocks.Remove(existing);
        }

        foreach (var stock in warehouseStocks) WarehouseStocks.Add(stock);
    }

    private void RefreshGroupOptions()
    {
        var selected = SelectedGroup;
        var options = Products
            .Select(product => product.GroupDisplayName)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group, StringComparer.CurrentCultureIgnoreCase)
            .Prepend(AllGroups)
            .ToArray();
        ReplaceItems(GroupOptions, options);
        SelectedGroup = options.Contains(selected, StringComparer.CurrentCultureIgnoreCase) ? selected : AllGroups;
    }
}
