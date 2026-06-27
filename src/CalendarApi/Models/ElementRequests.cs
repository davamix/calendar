using System.Text.RegularExpressions;

namespace CalendarApi.Models;

/// <summary>Payload used to create or fully update a calendar element.</summary>
/// <param name="Name">Required display name.</param>
/// <param name="Description">Optional free-text description.</param>
/// <param name="StartDate">Inclusive start day.</param>
/// <param name="EndDate">Inclusive end day; must be on or after <paramref name="StartDate"/>.</param>
/// <param name="Color">Optional hex colour, e.g. <c>#4f46e5</c> or <c>#abc</c>.</param>
public partial record ElementRequest(
    string? Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Color)
{
    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexColorRegex();

    /// <summary>Validates the payload, returning a map of field-&gt;error for any problems.</summary>
    public IDictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(Name))
            errors[nameof(Name)] = ["Name is required."];

        if (EndDate < StartDate)
            errors[nameof(EndDate)] = ["End date must be on or after the start date."];

        if (!string.IsNullOrWhiteSpace(Color) && !HexColorRegex().IsMatch(Color.Trim()))
            errors[nameof(Color)] = ["Color must be a hex code such as #4f46e5."];

        return errors;
    }

    /// <summary>
    /// Copies the (trimmed) request fields onto an element. Shared by the create factories
    /// and the store's update path so the two can never drift.
    /// </summary>
    public T ApplyTo<T>(T item) where T : CalendarItem
    {
        item.Name = Name!.Trim();
        item.Description = Description;
        item.StartDate = StartDate;
        item.EndDate = EndDate;
        item.Color = Color?.Trim();
        return item;
    }
}
