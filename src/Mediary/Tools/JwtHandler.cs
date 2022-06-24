namespace Mediary
{
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography;

    public class JwtCustomClaims
    {
        public string? Device { get; set; }
        public string? Domain { get; set; }
    }

    public class JwtResponse
    {
        public string? Token { get; set; }
        public long ExpiresAt { get; set; }
    }

    public static class TypeConverterExtension
    {
        public static byte[] ToByteArray(this string value) =>
         Convert.FromBase64String(value);
    }

    public static class JwtHandler
    {

        public static JwtResponse CreateToken(string key)
        {
            using ECDsa es = ECDsa.Create();
            es.ImportECPrivateKey(Convert.FromBase64String(key), out _);

            var signingCredentials = new SigningCredentials(new ECDsaSecurityKey(es), SecurityAlgorithms.EcdsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };

            var now = DateTime.Now;
            var unixTimeSeconds = new DateTimeOffset(now).ToUnixTimeSeconds();

            var jwt = new JwtSecurityToken(
                audience: "<YOUR_PROJECT_ID>",
                issuer: "<YOUR_PROJECT_ID>",
                claims: new Claim[] {
                    new Claim(JwtRegisteredClaimNames.Iat, unixTimeSeconds.ToString(), ClaimValueTypes.Integer64),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                },
                notBefore: now,
                expires: now.AddSeconds(20),
                signingCredentials: signingCredentials
            );

            string token = new JwtSecurityTokenHandler().WriteToken(jwt);

            return new JwtResponse
            {
                Token = token,
                ExpiresAt = unixTimeSeconds,
            };
        }

        public static bool ValidateToken(string token, string key)
        {

            using ECDsa es = ECDsa.Create();
            es.ImportFromPem(key);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "<YOUR_PROJECT_ID>",
                ValidAudience = "<YOUR_PROJECT_ID>",
                IssuerSigningKey = new ECDsaSecurityKey(es),

                CryptoProviderFactory = new CryptoProviderFactory()
                {
                    CacheSignatureProviders = false
                }
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(token, validationParameters, out var validatedSecurityToken);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}

