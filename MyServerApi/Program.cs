using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;

var myRsaKey = RSA.Create();
myRsaKey.ImportRSAPrivateKey(File.ReadAllBytes("myRsaKey"), out _);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("jwt")
    .AddJwtBearer("jwt", o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
        };

        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = (MessageReceivedContext ctx) =>
            {
                if (ctx.Request.Query.ContainsKey("t"))
                {
                    ctx.Token = ctx.Request.Query["t"];
                }

                return Task.CompletedTask;
            }
        };

        o.Configuration = new OpenIdConnectConfiguration
        {
            SigningKeys =
            {
                new RsaSecurityKey(myRsaKey)
            }
        };

        o.MapInboundClaims = false;
    });

var app = builder.Build();

app.UseAuthentication();

app.MapGet("/", (HttpContext ctx) => ctx.User.FindFirst("sub")?.Value ?? "empty");

app.MapGet("/jwt", () =>
{
    var handler = new JsonWebTokenHandler();
    var key = new RsaSecurityKey(myRsaKey);
    var token = handler.CreateToken(new SecurityTokenDescriptor()
    {
        Issuer = "https://localhost:5000",
        IssuedAt = DateTime.UtcNow,
        Subject = new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("username", "huehuehue br"),
        }),
        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
    });

    return token;
});

app.MapGet("/jwk", () =>
{
    var publicRsaKey = RSA.Create();
    publicRsaKey.ImportRSAPublicKey(myRsaKey.ExportRSAPublicKey(), out _);

    var publicKey = new RsaSecurityKey(publicRsaKey);
    return JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
});

app.MapGet("/jwk-private", () =>
{
    var privateKey = new RsaSecurityKey(myRsaKey);
    return JsonWebKeyConverter.ConvertFromRSASecurityKey(privateKey);

    // PrivateKey não deve ser exposta
});

app.Run();