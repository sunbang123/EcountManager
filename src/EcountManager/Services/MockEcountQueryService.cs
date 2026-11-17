using EcountManager.Models;

namespace EcountManager.Services;

public sealed class MockEcountQueryService : IEcountQueryService
{
    private static readonly IReadOnlyList<Customer> Customers =
    [
        new("C00018", "한빛테크", "김현수", "02-345-7812", "수도권 대리점", "박지민", "서울특별시 금천구 가산디지털로 142", 50_000_000m),
        new("C00031", "세진산업", "이서준", "031-721-4455", "제조 고객", "최유진", "경기도 성남시 중원구 둔촌대로 388", 30_000_000m),
        new("C00047", "대성솔루션", "윤민호", "051-802-9921", "영남 대리점", "정도윤", "부산광역시 부산진구 중앙대로 708", 80_000_000m)
    ];

    public async Task<IReadOnlyList<Customer>> SearchCustomersAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        await Task.Delay(120, cancellationToken);
        if (string.IsNullOrWhiteSpace(searchText)) return Customers;

        var term = searchText.Trim();
        return Customers.Where(customer =>
                customer.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                customer.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                customer.Phone.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public async Task<CustomerDashboard> GetCustomerDashboardAsync(string customerCode, CancellationToken cancellationToken = default)
    {
        await Task.Delay(180, cancellationToken);
        var customer = Customers.FirstOrDefault(x => x.Code == customerCode)
            ?? throw new InvalidOperationException("고객 정보를 찾을 수 없습니다.");

        return customerCode switch
        {
            "C00031" => CreateSejinDashboard(customer),
            "C00047" => CreateDaesungDashboard(customer),
            _ => CreateHanbitDashboard(customer)
        };
    }

    private static CustomerDashboard CreateHanbitDashboard(Customer customer)
    {
        var sales = new List<SaleRecord>
        {
            new("20261114-0032", new(2026, 11, 14), "P-10024", "산업용 온도센서", "-40~125℃ / 4-20mA", 12, "EA", 86_000, 1_032_000, 103_200, "이하늘"),
            new("20261105-0018", new(2026, 11, 5), "P-21008", "센서 연장 케이블", "M12 / 5m", 20, "EA", 19_500, 390_000, 39_000, "이하늘"),
            new("20261021-0007", new(2026, 10, 21), "P-31011", "디지털 표시기", "96x48 / AC220V", 4, "EA", 142_000, 568_000, 56_800, "박지민"),
            new("20260930-0041", new(2026, 9, 30), "P-10024", "산업용 온도센서", "-40~125℃ / 4-20mA", 10, "EA", 84_000, 840_000, 84_000, "박지민")
        };
        var inventory = new List<InventoryItem>
        {
            new("P-10024", "산업용 온도센서", "-40~125℃ / 4-20mA", "EA", 38, 92_000, 86_000, 12, new(2026, 11, 14), false),
            new("P-21008", "센서 연장 케이블", "M12 / 5m", "EA", 146, 21_000, 19_500, 20, new(2026, 11, 5), false),
            new("P-31011", "디지털 표시기", "96x48 / AC220V", "EA", 7, 155_000, 142_000, 4, new(2026, 10, 21), false),
            new("P-41003", "온도센서 브라켓", "SUS304 / L형", "EA", 0, 12_000, null, null, null, false)
        };
        var receivables = new List<ReceivableRecord>
        {
            new(new(2026, 10, 21), "매출", "20261021-0007", "디지털 표시기 외", 624_800, 0, 5_824_800, "박지민"),
            new(new(2026, 10, 31), "입금", "RC-261031-12", "기업은행 계좌입금", 0, 3_000_000, 2_824_800, "김서연"),
            new(new(2026, 11, 5), "매출", "20261105-0018", "센서 연장 케이블", 429_000, 0, 3_253_800, "이하늘"),
            new(new(2026, 11, 14), "매출", "20261114-0032", "산업용 온도센서", 1_135_200, 0, 4_389_000, "이하늘")
        };
        return new(customer, sales, inventory, receivables);
    }

    private static CustomerDashboard CreateSejinDashboard(Customer customer)
    {
        var sales = new List<SaleRecord>
        {
            new("20261112-0011", new(2026, 11, 12), "P-52001", "압력 트랜스미터", "0~10bar / G1/4", 6, "EA", 215_000, 1_290_000, 129_000, "최유진"),
            new("20261018-0026", new(2026, 10, 18), "P-52014", "압력 게이지", "0~16bar / 100Ø", 10, "EA", 48_000, 480_000, 48_000, "최유진")
        };
        var inventory = new List<InventoryItem>
        {
            new("P-52001", "압력 트랜스미터", "0~10bar / G1/4", "EA", 22, 228_000, 215_000, 6, new(2026, 11, 12), false),
            new("P-52014", "압력 게이지", "0~16bar / 100Ø", "EA", 64, 52_000, 48_000, 10, new(2026, 10, 18), false),
            new("P-52020", "압력 게이지", "0~25bar / 100Ø", "EA", 5, 54_000, null, null, null, false)
        };
        var receivables = new List<ReceivableRecord>
        {
            new(new(2026, 10, 18), "매출", "20261018-0026", "압력 게이지", 528_000, 0, 2_108_000, "최유진"),
            new(new(2026, 11, 12), "매출", "20261112-0011", "압력 트랜스미터", 1_419_000, 0, 3_527_000, "최유진")
        };
        return new(customer, sales, inventory, receivables);
    }

    private static CustomerDashboard CreateDaesungDashboard(Customer customer)
    {
        var sales = new List<SaleRecord>
        {
            new("20261115-0009", new(2026, 11, 15), "P-63002", "무선 데이터로거", "8CH / Wi-Fi", 3, "EA", 385_000, 1_155_000, 115_500, "정도윤"),
            new("20261007-0035", new(2026, 10, 7), "P-63007", "데이터로거 안테나", "2.4GHz / 3m", 8, "EA", 31_000, 248_000, 24_800, "정도윤")
        };
        var inventory = new List<InventoryItem>
        {
            new("P-63002", "무선 데이터로거", "8CH / Wi-Fi", "EA", 16, 410_000, 385_000, 3, new(2026, 11, 15), false),
            new("P-63007", "데이터로거 안테나", "2.4GHz / 3m", "EA", 92, 34_000, 31_000, 8, new(2026, 10, 7), false),
            new("P-63011", "데이터 수집 게이트웨이", "LAN / RS485", "EA", 9, 520_000, null, null, null, false)
        };
        var receivables = new List<ReceivableRecord>
        {
            new(new(2026, 10, 7), "매출", "20261007-0035", "데이터로거 안테나", 272_800, 0, 7_942_800, "정도윤"),
            new(new(2026, 10, 28), "입금", "RC-261028-08", "신한은행 계좌입금", 0, 5_000_000, 2_942_800, "김서연"),
            new(new(2026, 11, 15), "매출", "20261115-0009", "무선 데이터로거", 1_270_500, 0, 4_213_300, "정도윤")
        };
        return new(customer, sales, inventory, receivables);
    }
}
