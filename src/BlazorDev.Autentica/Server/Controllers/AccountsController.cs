using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BlazorDev.Autentica.Models.Models.InputModels;

namespace BlazorDev.Autentica.Server.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IConfiguration configuration;

        public AccountsController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.configuration = configuration;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterInputModel user)
        {
            IdentityUser identityUser = new IdentityUser
            {
                Email = user.Email,
                UserName = user.Email
            };

            IdentityResult identityResult = await userManager.CreateAsync(identityUser, user.Password);
            if (identityResult.Succeeded == true)
            {
                return StatusCode(StatusCodes.Status201Created, new { identityResult.Succeeded });
            }
            else
            {
                string errorsToReturn = "Registrazione fallita";
                foreach (var error in identityResult.Errors)
                {
                    errorsToReturn += Environment.NewLine;
                    errorsToReturn += $"Codice di errore: {error.Code}, {error.Description}";
                }
                return StatusCode(StatusCodes.Status500InternalServerError,
                    errorsToReturn);
            }
        }


        [HttpPost]
        [Route("signin")]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn([FromBody] RegisterInputModel user)
        {
            Microsoft.AspNetCore.Identity.SignInResult signInResult = await signInManager.PasswordSignInAsync
                (user.Email, user.Password, false, false);
            if (signInResult.Succeeded == true)
            {
                IdentityUser identityUser = await userManager.FindByEmailAsync(user.Email);
                string JSONWebTokenAsString = await GeneraJSONWebToken(identityUser);
                return Ok(JSONWebTokenAsString);
            }
            else
            {
                return Unauthorized(user);
            }
        }

        [NonAction]
        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<string> GeneraJSONWebToken(IdentityUser identityUser)
        {
            SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:SecurityKey"]));
            SigningCredentials credentials = new SigningCredentials(
                symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            IList<string> roleNames = await userManager.GetRolesAsync(identityUser);

            var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, identityUser.Id),
                    new Claim(ClaimTypes.Name, identityUser.Email),
                    new Claim(ClaimTypes.Email, identityUser.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, identityUser.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                }.Union(roleNames.Select(role => new Claim(ClaimTypes.Role, role)));

            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"],
                claims, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10),
                    credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }
    }
}
