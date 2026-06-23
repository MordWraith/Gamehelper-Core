namespace Shared.UpdateSecurity
{
    /// <summary>
    ///     RSA public key for manifest.sig verification.
    ///     Regenerate via scripts/ensure-update-signing-key.ps1
    /// </summary>
    internal static class UpdateSigningPublicKey
    {
        internal const string Pem = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAuMR6cjLlH/RCdD88Excc3/EacqH/mA0j
oxbedRf4uSzUJhSYwcA2jGDyXE//HlCpDXfzeofsEoCbTF4x21DIPWJQ3MKFJWSnorADiutJqEgt
Pcww7BFuqm58BoMngYwfl4TygxhZMCmmo5g1mdshRpN1M2iQZDC5uefUglwI/bbJUWukcGqejHYR
3YR3O4//Pa1xdyQPNAvePee1/eqoGIhsVQsp/cONcmIP8SSPQHm5yRT0o0cMz9GkGK9ZOS7TsFBG
XjXDfcAFGqrhxMC/VU6elJKk73eQYUWxznB+3CAqKxTEcAWcIQkV2yA9D9e9li/BQbxAQ6TPs6JA
At59ZQIDAQAB
-----END PUBLIC KEY-----";
    }
}