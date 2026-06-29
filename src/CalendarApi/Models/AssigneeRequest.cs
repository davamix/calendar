namespace CalendarApi.Models;

/// <summary>Payload to assign a user to an element (owner-only). The user is a Logto <c>sub</c>.</summary>
public record AssigneeRequest(string? UserId);
