using Microsoft.Extensions.Configuration;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;

namespace CareerOS.Api.Infrastructure.OpenBao;

public class OpenBaoConfigurationProvider : ConfigurationProvider
{
    private readonly OpenBaoOptions _options;

    public OpenBaoConfigurationProvider(OpenBaoOptions options)
    {
        _options = options;
    }

    public override void Load()
    {
        var address = _options.Address;
        var roleId = _options.RoleId;
        var secretId = _options.SecretId;
        var mountPoint = string.IsNullOrWhiteSpace(_options.MountPoint) ? "secret" : _options.MountPoint;

        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(secretId))
        {
            Console.WriteLine("[OpenBao] Warning: OpenBao is enabled but Address, RoleId, or SecretId is missing. Skipping OpenBao secrets loading.");
            return;
        }

        try
        {
            IAuthMethodInfo authMethod = new AppRoleAuthMethodInfo(roleId, secretId);
            var vaultClientSettings = new VaultClientSettings(address, authMethod);
            IVaultClient vaultClient = new VaultClient(vaultClientSettings);

            var fetchedData = FetchSecretsAsync(vaultClient, mountPoint).GetAwaiter().GetResult();
            if (fetchedData.Count > 0)
            {
                Data = fetchedData;
                Console.WriteLine($"[OpenBao] Successfully loaded {fetchedData.Count} secret entries from OpenBao at {address}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenBao] Warning: Failed to retrieve secrets from OpenBao at '{address}': {ex.Message}. Falling back to default configuration.");
        }
    }

    private static async Task<Dictionary<string, string?>> FetchSecretsAsync(IVaultClient vaultClient, string mountPoint)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // Path 1: careeros/database -> connectionstring
        try
        {
            var dbSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: "careeros/database", mountPoint: mountPoint);
            if (dbSecret?.Data?.Data != null && TryGetDictValue(dbSecret.Data.Data, "connectionstring", out var connStr) && !string.IsNullOrWhiteSpace(connStr))
            {
                SetKey(result, "ConnectionStrings:CareerDatabase", connStr);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenBao] Warning: Could not read secret 'careeros/database': {ex.Message}");
        }

        // Path 2: careeros/jwt -> secretkey
        try
        {
            var jwtSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: "careeros/jwt", mountPoint: mountPoint);
            if (jwtSecret?.Data?.Data != null && TryGetDictValue(jwtSecret.Data.Data, "secretkey", out var secretKey) && !string.IsNullOrWhiteSpace(secretKey))
            {
                SetKey(result, "JwtOptions:SecretKey", secretKey);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenBao] Warning: Could not read secret 'careeros/jwt': {ex.Message}");
        }

        // Path 3: careeros/auth-google -> clientid, clientsecret
        try
        {
            var googleSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: "careeros/auth-google", mountPoint: mountPoint);
            if (googleSecret?.Data?.Data != null)
            {
                if (TryGetDictValue(googleSecret.Data.Data, "clientid", out var clientId) && !string.IsNullOrWhiteSpace(clientId))
                {
                    SetKey(result, "Authentication:Google:ClientId", clientId);
                }
                if (TryGetDictValue(googleSecret.Data.Data, "clientsecret", out var clientSecret) && !string.IsNullOrWhiteSpace(clientSecret))
                {
                    SetKey(result, "Authentication:Google:ClientSecret", clientSecret);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenBao] Warning: Could not read secret 'careeros/auth-google': {ex.Message}");
        }

        return result;
    }

    private static void SetKey(Dictionary<string, string?> dict, string colonKey, string value)
    {
        dict[colonKey] = value;
        var doubleUnderscoreKey = colonKey.Replace(":", "__");
        dict[doubleUnderscoreKey] = value;
    }

    private static bool TryGetDictValue(IDictionary<string, object> dict, string key, out string? value)
    {
        value = null;
        foreach (var kvp in dict)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kvp.Value?.ToString();
                return true;
            }
        }
        return false;
    }
}
