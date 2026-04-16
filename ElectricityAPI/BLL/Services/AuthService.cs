using BLL.Models;
using Core.Entities;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BLL.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;
        private readonly RefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserRepository userRepository,
            RefreshTokenRepository refreshTokenRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponseDTO> RegisterAsync(UserRegisterDto request)
        {
            ValidateRegisterCredentials(request.Email, request.Password, request.Username);

            string email = request.Email.Trim().ToLowerInvariant();
            string username = request.Username.Trim();

            if (await _userRepository.ExistsByEmailAsync(email))
            {
                throw new InvalidOperationException("Email is already in use.");
            }

            if (await _userRepository.ExistsByUsernameAsync(username))
            {
                throw new InvalidOperationException("Username is already in use.");
            }

            User user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(request.Password),
                IsAdmin = false,
                CreatedAt = KyivTimeHelper.Now
            };

            await _userRepository.AddAsync(user);
            try
            {
                await _userRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Email or username is already in use.");
            }

            return await CreateAndPersistTokensAsync(user);
        }

        public async Task<AuthResponseDTO> LoginAsync(UserLoginDTO request)
        {
            ValidateLoginCredentials(request.Email, request.Password);

            string email = request.Email.Trim().ToLowerInvariant();
            User? user = await _userRepository.GetByEmailAsync(email);

            if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            return await CreateAndPersistTokensAsync(user);
        }

        public async Task<AuthResponseDTO> RefreshAsync(RefreshTokenRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new ArgumentException("Refresh token is required.");
            }

            string refreshTokenHash = HashRefreshToken(request.RefreshToken);
            RefreshToken? storedToken = await _refreshTokenRepository.GetByHashAsync(refreshTokenHash);

            if (storedToken is null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            if (storedToken.RevokedAt is not null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            DateTime kyivNow = KyivTimeHelper.Now;

            if (storedToken.ExpiresAt <= kyivNow)
            {
                await _refreshTokenRepository.TryRevokeAsync(storedToken.Id, kyivNow);
                throw new UnauthorizedAccessException("Refresh token expired.");
            }

            int affectedRows = await _refreshTokenRepository.TryRevokeAsync(storedToken.Id, kyivNow);
            if (affectedRows == 0)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            return await CreateAndPersistTokensAsync(storedToken.User);
        }

        public async Task<List<UserDTO>> GetUsersAsync()
        {
            List<User> users = await _userRepository.GetAllAsync();

            return users.Select(u => new UserDTO
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email
            }).ToList();
        }

        private async Task<AuthResponseDTO> CreateAndPersistTokensAsync(User user)
        {
            string? jwtKey = _configuration["Jwt:Key"];
            string? jwtIssuer = _configuration["Jwt:Issuer"];
            string? jwtAudience = _configuration["Jwt:Audience"];
            int accessExpiresMinutes = int.TryParse(_configuration["Jwt:ExpiresMinutes"], out int parsedMinutes)
                ? parsedMinutes
                : 60;
            int refreshExpiresDays = int.TryParse(_configuration["Jwt:RefreshExpiresDays"], out int parsedDays)
                ? parsedDays
                : 14;

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("Jwt:Key is not configured.");
            }

            DateTime accessExpiresAtUtc = DateTime.UtcNow.AddMinutes(accessExpiresMinutes);
            string accessToken = CreateAccessToken(user, jwtKey, jwtIssuer, jwtAudience, accessExpiresAtUtc);

            string refreshToken = GenerateRefreshToken();
            string refreshTokenHash = HashRefreshToken(refreshToken);

            RefreshToken refreshTokenEntity = new RefreshToken
            {
                TokenHash = refreshTokenHash,
                UserId = user.Id,
                CreatedAt = KyivTimeHelper.Now,
                ExpiresAt = KyivTimeHelper.Now.AddDays(refreshExpiresDays)
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);
            await _refreshTokenRepository.SaveChangesAsync();

            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAtUtc = accessExpiresAtUtc,
                Role = user.IsAdmin ? "Admin" : "User",
                User = new UserDTO
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email
                }
            };
        }

        private static string CreateAccessToken(User user, string jwtKey, string? jwtIssuer, string? jwtAudience, DateTime expiresAtUtc)
        {
            List<Claim> claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.UniqueName, user.Username),
                new(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            };

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static void ValidateRegisterCredentials(string email, string password, string username)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("All fields are required.");
            }

            if (username.Trim().Length < 3)
            {
                throw new ArgumentException("Username must be at least 3 characters.");
            }

            if (password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters.");
            }
        }

        private static void ValidateLoginCredentials(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("All fields are required.");
            }

            if (password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters.");
            }
        }

        private static string HashPassword(string password)
        {
            const int iterations = 100_000;
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);

            return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            string[] parts = storedHash.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out int iterations))
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expectedHash = Convert.FromBase64String(parts[2]);
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        private static string GenerateRefreshToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }

        private static string HashRefreshToken(string refreshToken)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(refreshToken);
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

    }
}
