using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using EcountManager.Models;

namespace EcountManager.Services;

public sealed class EcountApiClient(HttpClient httpClient) : IEcountApiClient
{
    private const string ZonePath = "/OAPI/V2/Zone";
    private const string LoginPath = "/OAPI/V2/OAPILogin";
    private const string ProductsPath = "/OAPI/V2/InventoryBasic/GetBasicProductsList";
    private const string InventoryPath = "/OAPI/V2/InventoryBalance/GetListInventoryBalanceStatus";
    private const string WarehouseInventoryPath = "/OAPI/V2/InventoryBalance/GetListInventoryBalanceStatusByLocation";

    public async Task<EcountSession> ConnectAsync(EcountConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        var commonHost = settings.UseTestServer ? "https://sboapi.ecount.com" : "https://oapi.ecount.com";
        using var zoneDocument = await PostAsync(
            $"{commonHost}{ZonePath}",
            new { COM_CODE = settings.CompanyCode },
            cancellationToken);

        var zone = FindFirstString(zoneDocument.RootElement, "ZONE");
        if (string.IsNullOrWhiteSpace(zone))
        {
            throw CreateApiException(zoneDocument.RootElement, "Ecount Zone 정보를 받지 못했습니다.");
        }

        var zoneHost = settings.UseTestServer
            ? $"https://sboapi{zone}.ecount.com"
            : $"https://oapi{zone}.ecount.com";

        using var loginDocument = await PostAsync(
            $"{zoneHost}{LoginPath}",
            new
            {
                COM_CODE = settings.CompanyCode,
                USER_ID = settings.UserId,
                API_CERT_KEY = settings.ApiCertificateKey,
                LAN_TYPE = "ko-KR",
                ZONE = zone
            },
            cancellationToken);

        var sessionId = FindFirstString(loginDocument.RootElement, "SESSION_ID");
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw CreateApiException(loginDocument.RootElement, "Ecount 로그인 세션을 발급받지 못했습니다.");
        }

        return new(zone, sessionId, settings.UseTestServer, DateTimeOffset.Now);
    }

    public async Task<IReadOnlyList<EcountProduct>> GetProductsAsync(EcountSession session, CancellationToken cancellationToken = default)
    {
        using var document = await PostSessionAsync(
            session,
            ProductsPath,
            new { PROD_TYPE = string.Empty, FROM_PROD_CD = string.Empty, TO_PROD_CD = string.Empty },
            cancellationToken);

        var records = FindRecordObjects(document.RootElement, "PROD_CD");
        return records
            .Select(record => new EcountProduct
            {
                Code = GetString(record, "PROD_CD"),
                Name = GetString(record, "PROD_DES"),
                Specification = GetString(record, "SIZE_DES"),
                Unit = GetString(record, "UNIT"),
                ProductType = GetString(record, "PROD_TYPE"),
                Barcode = GetString(record, "BAR_CODE"),
                PurchasePrice = GetDecimal(record, "IN_PRICE"),
                SalesPrice = GetDecimal(record, "OUT_PRICE"),
                TaxRate = GetDecimal(record, "TAX")
            })
            .Where(product => !string.IsNullOrWhiteSpace(product.Code))
            .GroupBy(product => product.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(product => product.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetInventoryAsync(
        EcountSession session,
        DateOnly baseDate,
        CancellationToken cancellationToken = default)
    {
        using var document = await PostSessionAsync(
            session,
            InventoryPath,
            new { BASE_DATE = baseDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture), PROD_CD = string.Empty },
            cancellationToken);

        return FindRecordObjects(document.RootElement, "PROD_CD")
            .Where(record => record.TryGetProperty("BAL_QTY", out _))
            .GroupBy(record => GetString(record, "PROD_CD"), StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToDictionary(group => group.Key, group => group.Sum(record => GetDecimal(record, "BAL_QTY")), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<EcountWarehouseStock>> GetWarehouseInventoryAsync(
        EcountSession session,
        DateOnly baseDate,
        CancellationToken cancellationToken = default)
    {
        using var document = await PostSessionAsync(
            session,
            WarehouseInventoryPath,
            new
            {
                BASE_DATE = baseDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                PROD_CD = string.Empty,
                WH_CD = string.Empty
            },
            cancellationToken);

        return FindRecordObjects(document.RootElement, "PROD_CD")
            .Where(record => record.TryGetProperty("WH_CD", out _) && record.TryGetProperty("BAL_QTY", out _))
            .Select(record => new EcountWarehouseStock(
                GetString(record, "WH_CD"),
                GetString(record, "WH_DES"),
                GetString(record, "PROD_CD"),
                GetString(record, "PROD_DES"),
                GetString(record, "SIZE_DES"),
                GetDecimal(record, "BAL_QTY")))
            .ToArray();
    }

    private async Task<JsonDocument> PostSessionAsync(EcountSession session, string path, object body, CancellationToken cancellationToken)
    {
        var host = session.IsTestServer
            ? $"https://sboapi{session.Zone}.ecount.com"
            : $"https://oapi{session.Zone}.ecount.com";
        var sessionId = Uri.EscapeDataString(session.SessionId);
        return await PostAsync($"{host}{path}?SESSION_ID={sessionId}", body, cancellationToken);
    }

    private async Task<JsonDocument> PostAsync(string url, object body, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(url, body, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new EcountApiException($"Ecount 서버가 빈 응답을 반환했습니다. HTTP {(int)response.StatusCode}");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new EcountApiException($"Ecount 응답을 해석할 수 없습니다. HTTP {(int)response.StatusCode}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var exception = CreateApiException(document.RootElement, $"Ecount API 요청이 실패했습니다. HTTP {(int)response.StatusCode}");
            document.Dispose();
            throw exception;
        }

        var status = FindFirstString(document.RootElement, "Status");
        if (string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "Fail", StringComparison.OrdinalIgnoreCase))
        {
            var exception = CreateApiException(document.RootElement, "Ecount API가 오류를 반환했습니다.");
            document.Dispose();
            throw exception;
        }

        return document;
    }

    private static EcountApiException CreateApiException(JsonElement root, string fallbackMessage)
    {
        var message = FindFirstString(root, "Message")
            ?? FindFirstString(root, "Errors")
            ?? FindFirstString(root, "ErrorMessage")
            ?? fallbackMessage;
        var code = FindFirstString(root, "Code") ?? FindFirstString(root, "ErrorCode");
        return new(message, code);
    }

    private static string? FindFirstString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.Value.ToString(),
                        _ => null
                    };
                }

                var nested = FindFirstString(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindFirstString(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
        }

        return null;
    }

    private static IReadOnlyList<JsonElement> FindRecordObjects(JsonElement root, string requiredProperty)
    {
        var results = new List<JsonElement>();
        Visit(root);
        return results;

        void Visit(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(requiredProperty, out _))
                {
                    results.Add(element);
                    return;
                }

                foreach (var property in element.EnumerateObject()) Visit(property.Value);
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray()) Visit(item);
            }
        }
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)) return string.Empty;
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
    }

    private static decimal GetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)) return 0;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number) ? number : 0;
    }
}
