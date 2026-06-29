using CalendarApi.Models;
using FluentAssertions;
using Xunit;

namespace CalendarApi.UnitTests;

public sealed class ElementRequestTests
{
    private static ElementRequest Valid() =>
        new("Launch", "desc", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "#abc");

    [Fact]
    public void Validate_ValidRequest_NoErrors() =>
        Valid().Validate().Should().BeEmpty();

    [Fact]
    public void Validate_MissingName_ReportsNameError() =>
        (Valid() with { Name = "  " }).Validate().Should().ContainKey(nameof(ElementRequest.Name));

    [Fact]
    public void Validate_EndBeforeStart_ReportsEndDateError() =>
        (Valid() with { EndDate = new DateOnly(2026, 5, 1) }).Validate()
            .Should().ContainKey(nameof(ElementRequest.EndDate));

    [Fact]
    public void Validate_BadColor_ReportsColorError() =>
        (Valid() with { Color = "red" }).Validate().Should().ContainKey(nameof(ElementRequest.Color));

    [Fact]
    public void Validate_NoColor_IsAllowed() =>
        (Valid() with { Color = null }).Validate().Should().NotContainKey(nameof(ElementRequest.Color));
}
