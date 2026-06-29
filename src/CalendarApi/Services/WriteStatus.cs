namespace CalendarApi.Services;

/// <summary>Outcome of an owner-gated write, so endpoints can distinguish 404 from 403.</summary>
public enum WriteStatus
{
    Success,
    NotFound,   // not visible to the caller (filtered) or doesn't exist
    Forbidden,  // visible but the caller is not the owner
}
