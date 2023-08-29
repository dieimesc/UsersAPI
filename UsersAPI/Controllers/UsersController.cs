using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Users.Domain.Models;
using UsersAPI.Infra.Data;

namespace UsersAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserDBContext _context;
        private readonly JWTSettings _jwtsettings;

        public UsersController(UserDBContext context, IOptions<JWTSettings> jwtsettings)
        {
            _context = context;
            _jwtsettings = jwtsettings.Value;
        }


        // POST: api/users
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }


        // POST: api/login
        [AllowAnonymous]
        [HttpPost("login")]
        
        public async Task<ActionResult<object>> Login([FromBody] User user)
        {
            try
            {
                user = await _context.Users
                                            // .Include(u => u.Role)
                                            .Where(u => u.Login == user.Login
                                                    && u.Password == user.Password).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro de servidor: {ex.Message}");
            }
                       
            if (user == null)
            {
                return Unauthorized("Usuário inexistente ou senha incorreta");
            }           

            //Gera a assinatura do Token
            var token = GenerateAccessToken(user.Id);           
            return new
            {
                user,
                token
            };
        }
        
        // GET: api/users/me
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<User>> GetUserByAccessToken(string token)
        {
            try
            {
                // Pega o usuário através do token informado
                User user = await GetUserFromAccessToken(token);
                if (user != null)                                                   
                {
                    return user;
                }

                return NotFound("Usuário não encontrado");
            }
            catch(Exception ex)
            {
                return BadRequest($"Erro de servidor:{ ex.Message }");
            }

            
        }                           

        private async Task<User> GetUserFromAccessToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };

                SecurityToken securityToken;
                var principle = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

                JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken != null && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    var userId = principle.FindFirst(ClaimTypes.Name)?.Value;

                    return await _context.Users //.Include(u => u.Role)
                                        .Where(u => u.Id == Convert.ToInt32(userId)).FirstOrDefaultAsync();
                }
            }
            catch (Exception)
            {
                return null;
            }

            return new User();
        }
        private string GenerateAccessToken(int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
           
            //Pega a key(O ideal é que obtenha um secret de algum server, nesta implementalção, está no appsettings por conveniÊncia
            var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);
           
            // Realiza a assinatura com data de expiração
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, Convert.ToString(userId))
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }       
        private bool UserExists(int id)
        {
            return _context.Users.Any(user => user.Id == id);
        }
    }
}
