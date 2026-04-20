namespace backend.Dtos;

public record CreateTicketRequest(
    string Issue,
    string Category,
    string? Location,
    string Priority);

public record UpdateTicketRequest(string Status, string Priority, string? AssignedTo);

public record TicketResponse(
    int Id,
    string Issue,
    string Category,
    string? Location,
    string Priority,
    string Status,
    DateTime CreatedAt,
    int CreatedBy,
    string? CreatedByName,
    string? AssignedTo);
