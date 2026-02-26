using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class AutenticacionStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _storage;
    private AuthenticationState _estadoActual =
        new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

    public AutenticacionStateProvider(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var resultado = await _storage.GetAsync<string>("token");
            var token = resultado.Success ? resultado.Value : null;

            if (string.IsNullOrWhiteSpace(token))
                return _estadoActual;

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var claims = jwt.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            _estadoActual = new AuthenticationState(user);
        }
        catch
        {
            _estadoActual = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        return _estadoActual;
    }

    public async Task NotifyUserAuthentication(string token, string email)
    {
        await _storage.SetAsync("token", token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var claims = jwt.Claims.ToList();
        claims.Add(new Claim(ClaimTypes.Email, email));

        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        _estadoActual = new AuthenticationState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(_estadoActual));
    }

    public async Task NotifyUserLogout()
    {
        await _storage.DeleteAsync("token");
        _estadoActual = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        NotifyAuthenticationStateChanged(Task.FromResult(_estadoActual));
    }
}
