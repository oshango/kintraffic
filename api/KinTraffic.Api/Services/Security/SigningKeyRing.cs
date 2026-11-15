using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace KinTraffic.Api.Services.Security;

public sealed class SigningKeyRing : IDisposable
{
    private readonly List<RSA> _keys;

    public SigningKeyRing(AuthenticationOptions options)
    {
        _keys = [];
        var validationKeys = new Dictionary<string, SecurityKey>(StringComparer.Ordinal);
        ValidationKeys = validationKeys;

        var currentRsa = ImportRsa(options.SigningPrivateKeyPem!);
        _keys.Add(currentRsa);
        var currentKey = new RsaSecurityKey(currentRsa) { KeyId = options.SigningKeyId };
        CurrentSigningCredentials = new SigningCredentials(currentKey, SecurityAlgorithms.RsaSha256);
        validationKeys.Add(currentKey.KeyId!, currentKey);

        foreach (var previous in options.PreviousSigningKeys ?? [])
        {
            var previousRsa = ImportRsa(previous.PublicKeyPem!);
            _keys.Add(previousRsa);
            validationKeys.Add(previous.KeyId!, new RsaSecurityKey(previousRsa) { KeyId = previous.KeyId });
        }
    }

    public SigningCredentials CurrentSigningCredentials { get; }
    public IReadOnlyDictionary<string, SecurityKey> ValidationKeys { get; }

    public void Dispose()
    {
        foreach (var key in _keys)
        {
            key.Dispose();
        }
    }

    private static RSA ImportRsa(string pem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return rsa;
    }
}