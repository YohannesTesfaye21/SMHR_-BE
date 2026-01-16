using SMHFR_BE.Models;

namespace SMHFR_BE.Services;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
}
