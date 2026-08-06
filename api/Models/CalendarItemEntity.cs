using Azure;
using Azure.Data.Tables;

namespace CalendarApi.Models;

// Azure Table Storage requires PartitionKey/RowKey. We use a single
// fixed partition ("item") since the whole dataset is small (a family
// calendar's worth of events), and RowKey as the item's unique Id.
public class CalendarItemEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "item";
    public string RowKey { get; set; } = "";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Start { get; set; } = "";
    public string? End { get; set; }
    public string Type { get; set; } = "event";
    public string Title { get; set; } = "";
    public string? Note { get; set; }

    public static CalendarItemEntity FromModel(CalendarItem m) => new()
    {
        PartitionKey = "item",
        RowKey = m.Id,
        Start = m.Start,
        End = m.End,
        Type = m.Type,
        Title = m.Title,
        Note = m.Note
    };

    public CalendarItem ToModel() => new()
    {
        Id = RowKey,
        Start = Start,
        End = End,
        Type = Type,
        Title = Title,
        Note = Note
    };
}
