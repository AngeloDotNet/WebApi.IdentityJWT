using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BlazorDev.Autentica.Shared.Models.Services.Application
{
    public class AppAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService localStorageService;
        private readonly JwtSecurityTokenHandler jwtSecurityTokenHandler = new();

        public AppAuthenticationStateProvider(ILocalStorageService localStorageService)
        {
            this.localStorageService = localStorageService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                string savedToken = await localStorageService.GetItemAsync<string>("bearerToken");

                if (string.IsNullOrWhiteSpace(savedToken))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                JwtSecurityToken jwtSecurityToken = jwtSecurityTokenHandler.ReadJwtToken(savedToken);
                DateTime expires = jwtSecurityToken.ValidTo;

                if (expires < DateTime.UtcNow)
                {
                    await localStorageService.RemoveItemAsync("bearerToken");

                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                IList<Claim> claims = jwtSecurityToken.Claims.ToList();
                claims.Add(new Claim(ClaimTypes.Name, jwtSecurityToken.Subject));

                var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
                return new AuthenticationState(user);

            }
            catch (Exception)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task SignIn()
        {
            string savedToken = await localStorageService.GetItemAsStringAsync("bearerToken");

            JwtSecurityToken jwtSecurityToken = jwtSecurityTokenHandler.ReadJwtToken(savedToken.Substring(1, savedToken.Length - 2));

            IList<Claim> claims = jwtSecurityToken.Claims.ToList();
            
            claims.Add(new Claim(ClaimTypes.Name, jwtSecurityToken.Subject));
            
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            
            Task<AuthenticationState> authentication = Task.FromResult(new AuthenticationState(user));
            
            NotifyAuthenticationStateChanged(authentication);
        }

        public void SignOut()
        {
            ClaimsPrincipal nobody = new ClaimsPrincipal(new ClaimsIdentity());
            
            Task<AuthenticationState> authentication = Task.FromResult(new AuthenticationState(nobody));
            
            NotifyAuthenticationStateChanged(authentication);
        }
    }
}
