using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using EcountManager.Models;
using EcountManager.Services;

namespace EcountManager.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
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

    public MainWindowViewModel(IEcountApiClient apiClient)
    {
        _apiClient = apiClient;
        ProductsView = CollectionViewSource.GetDefaultView(Products);
        WarehouseStocksView = CollectionViewSource.GetDefaultView(WarehouseStocks);
        ProductsView.Filter = FilterProduct;
        WarehouseStocksView.Filter = FilterWarehouseStock;
        ConnectCommand = new AsyncRelayCommand(ConnectAndSyncAsync, CanConnect);
        RefreshCommand = new AsyncRelayCommand(SyncAsync, () => _session is not null && !IsBusy);
    }

    public ObservableCollection<EcountProduct> Products { get; } = [];
    public ObservableCollection<EcountWarehouseStock> WarehouseStocks { get; } = [];
    public ICollectionView ProductsView { get; }
    public ICollectionView WarehouseStocksView { get; }
    public ICommand ConnectCommand { get; }
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
            if (SetProperty(ref _searchText, value))
            {
                ProductsView.Refresh();
                WarehouseStocksView.Refresh();
            }
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
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

    private async Task ConnectAndSyncAsync()
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
            await SyncCoreAsync();
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

    private async Task SyncAsync()
    {
        try
        {
            IsBusy = true;
            await SyncCoreAsync();
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"동기화 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SyncCoreAsync()
    {
        if (_session is null) return;
        var baseDate = DateOnly.FromDateTime(DateTime.Today);

        ConnectionStatus = "품목 정보를 불러오는 중...";
        var products = await _apiClient.GetProductsAsync(_session);
        ConnectionStatus = "재고 현황을 불러오는 중...";
        var inventory = await _apiClient.GetInventoryAsync(_session, baseDate);
        foreach (var product in products)
        {
            if (inventory.TryGetValue(product.Code, out var quantity)) product.CurrentStock = quantity;
        }

        ConnectionStatus = "창고별 재고를 불러오는 중...";
        var warehouseStocks = await _apiClient.GetWarehouseInventoryAsync(_session, baseDate);
        ReplaceItems(Products, products);
        ReplaceItems(WarehouseStocks, warehouseStocks);
        LastSyncedAt = DateTimeOffset.Now;
        NotifySummary();
        ConnectionStatus = $"Ecount 연결 완료 · {products.Count:N0}개 품목";
    }

    private bool FilterProduct(object item)
    {
        if (item is not EcountProduct product || string.IsNullOrWhiteSpace(SearchText)) return true;
        var term = SearchText.Trim();
        return product.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
            || product.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || product.Specification.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || product.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterWarehouseStock(object item)
    {
        if (item is not EcountWarehouseStock stock || string.IsNullOrWhiteSpace(SearchText)) return true;
        var term = SearchText.Trim();
        return stock.ProductCode.Contains(term, StringComparison.OrdinalIgnoreCase)
            || stock.ProductName.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || stock.WarehouseName.Contains(term, StringComparison.CurrentCultureIgnoreCase);
    }

    private void RaiseCommandStates()
    {
        ((AsyncRelayCommand)ConnectCommand).RaiseCanExecuteChanged();
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
}
