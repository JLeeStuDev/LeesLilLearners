using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using CalendarApi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CalendarApi.Functions;

public class CalendarItemsFunctions
{
    private readonly TableClient _table;
    private readonly ILogger _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public CalendarItemsFunctions(TableClient table, ILoggerFactory loggerFactory)
    {
        _table = table;
        _logger = loggerFactory.CreateLogger<CalendarItemsFunctions>();
    }

    // GET /api/items  -> list every custom calendar item
    [Function("GetItems")]
    public async Task<HttpResponseData> GetItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "items")] HttpRequestData req)
    {
        var results = new List<CalendarItem>();
        await foreach (var entity in _table.QueryAsync<CalendarItemEntity>(e => e.PartitionKey == "item"))
        {
            results.Add(entity.ToModel());
        }
        results.Sort((a, b) => string.Compare(a.Start, b.Start, StringComparison.Ordinal));

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(results);
        return response;
    }

    // POST /api/items  -> create a new item, body: { start, end, type, title, note }
    [Function("CreateItem")]
    public async Task<HttpResponseData> CreateItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "items")] HttpRequestData req)
    {
        CalendarItem? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CalendarItem>(req.Body, JsonOpts);
        }
        catch (JsonException)
        {
            return await BadRequest(req, "Request body was not valid JSON.");
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Start) || string.IsNullOrWhiteSpace(body.Title))
        {
            return await BadRequest(req, "\"start\" and \"title\" are required.");
        }

        body.Id = Guid.NewGuid().ToString("N");
        var entity = CalendarItemEntity.FromModel(body);
        await _table.AddEntityAsync(entity);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(body);
        return response;
    }

    // PUT /api/items/{id}  -> update an existing item
    [Function("UpdateItem")]
    public async Task<HttpResponseData> UpdateItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "items/{id}")] HttpRequestData req,
        string id)
    {
        CalendarItem? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CalendarItem>(req.Body, JsonOpts);
        }
        catch (JsonException)
        {
            return await BadRequest(req, "Request body was not valid JSON.");
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Start) || string.IsNullOrWhiteSpace(body.Title))
        {
            return await BadRequest(req, "\"start\" and \"title\" are required.");
        }

        body.Id = id;
        var entity = CalendarItemEntity.FromModel(body);

        try
        {
            await _table.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(body);
        return response;
    }

    // DELETE /api/items/{id}
    [Function("DeleteItem")]
    public async Task<HttpResponseData> DeleteItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "items/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            await _table.DeleteEntityAsync("item", id);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Already gone is fine for a delete.
        }
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}
