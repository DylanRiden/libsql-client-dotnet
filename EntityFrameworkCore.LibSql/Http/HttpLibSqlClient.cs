using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace EntityFrameworkCore.LibSql.Http;

public class HttpLibSqlClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _authToken;
    private bool _disposed;

    public HttpLibSqlClient(string url, string authToken)
    {
        _baseUrl = url.TrimEnd('/');
        _authToken = authToken;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
    }

    public async Task<LibSqlResult> ExecuteAsync(string sql, object[]? parameters = null)
    {
        var request = new LibSqlRequest
        {
            Statements = new[]
            {
                new LibSqlStatement
                {
                    Query = sql,
                    Params = parameters ?? Array.Empty<object>()
                }
            }
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        Console.WriteLine($"DEBUG HTTP LibSQL: Executing SQL: {sql}");
        if (parameters?.Length > 0)
        {
            Console.WriteLine($"DEBUG HTTP LibSQL: Parameters: {string.Join(", ", parameters)}");
        }

        var response = await _httpClient.PostAsync($"{_baseUrl}/v1/execute", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"LibSQL HTTP request failed: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"DEBUG HTTP LibSQL: Response: {responseJson}");
        
        var result = JsonSerializer.Deserialize<LibSqlResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new LibSqlResult(result?.Results?.FirstOrDefault());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

// Request/Response DTOs
public class LibSqlRequest
{
    public LibSqlStatement[] Statements { get; set; } = Array.Empty<LibSqlStatement>();
}

public class LibSqlStatement
{
    public string Query { get; set; } = string.Empty;
    public object[] Params { get; set; } = Array.Empty<object>();
}

public class LibSqlResponse
{
    public LibSqlResultData[] Results { get; set; } = Array.Empty<LibSqlResultData>();
}

public class LibSqlResultData
{
    public string[] Columns { get; set; } = Array.Empty<string>();
    public object[][] Rows { get; set; } = Array.Empty<object[]>();
    public int RowsAffected { get; set; }
    public long? LastInsertRowid { get; set; }
}

// Wrapper to match the interface expected by your current code
public class LibSqlResult
{
    private readonly LibSqlResultData? _data;

    public LibSqlResult(LibSqlResultData? data)
    {
        _data = data;
    }

    public string[] Columns => _data?.Columns ?? Array.Empty<string>();
    public IEnumerable<object[]> Rows => _data?.Rows ?? Array.Empty<object[]>();
    public int RowsAffected => _data?.RowsAffected ?? 0;
    public long? LastInsertRowid => _data?.LastInsertRowid;
}