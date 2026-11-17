namespace EcountManager.Models;

public sealed record EcountConnectionSettings(
    string CompanyCode,
    string UserId,
    string ApiCertificateKey,
    bool UseTestServer);

public sealed record EcountSession(
    string Zone,
    string SessionId,
    bool IsTestServer,
    DateTimeOffset ConnectedAt);

