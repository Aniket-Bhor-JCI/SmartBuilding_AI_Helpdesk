using backend.Models;

namespace backend.Services;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
