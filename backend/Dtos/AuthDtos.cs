namespace backend.Dtos;

public record RegisterRequest(string Name, string Email, string Password, string Role);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, string Name, string Email, string Role);
