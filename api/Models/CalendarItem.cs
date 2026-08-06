namespace CalendarApi.Models;

// The shape sent to / returned from the frontend.
public class CalendarItem
{
    public string Id { get; set; } = "";
    public string Start { get; set; } = "";   // yyyy-MM-dd
    public string? End { get; set; }          // yyyy-MM-dd, optional
    public string Type { get; set; } = "event"; // "event" | "absence" | "note"
    public string Title { get; set; } = "";
    public string? Note { get; set; }
}
