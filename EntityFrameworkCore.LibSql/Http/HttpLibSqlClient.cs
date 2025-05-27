using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EntityFrameworkCore.LibSql.Http;

public class HttpLibSqlClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _authToken;
    private bool _disposed;

    public HttpLibSqlClient(string url, string authToken)
    {
        _authToken = authToken;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        
        // Convert libsql:// to https:// for HTTP requests
        _baseUrl = ConvertLibSqlUrlToHttps(url);
        
        Console.WriteLine($"DEBUG HTTP LibSQL: Using base URL: {_baseUrl}");
    }

    private static string ConvertLibSqlUrlToHttps(string url)
    {
        if (url.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase))
        {
            // Convert libsql://example.turso.io to https://example.turso.io
            return "https://" + url.Substring("libsql://".Length);
        }
        
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }
        
        // If it doesn't start with a scheme, assume it needs https://
        if (!url.Contains("://"))
        {
            return "https://" + url;
        }
        
        return url;
    }

    public async Task<LibSqlResult> ExecuteAsync(string sql, object[]? parameters = null)
    {
        // Use Turso's v2/pipeline API format
        var stmt = new LibSqlStatement { Sql = sql };
        
        // Only add args if there are parameters (don't set to null)
        var args = ConvertParametersToArgs(parameters);
        if (args != null && args.Length > 0)
        {
            stmt.Args = args;
        }
        
        var request = new LibSqlRequest
        {
            Requests = new[]
            {
                new LibSqlPipelineRequest
                {
                    Type = "execute",
                    Stmt = stmt
                },
                new LibSqlPipelineRequest
                {
                    Type = "close"
                }
            }
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        
        Console.WriteLine($"DEBUG HTTP LibSQL: Request JSON: {json}");

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        Console.WriteLine($"DEBUG HTTP LibSQL: Executing SQL: {sql}");
        if (parameters?.Length > 0)
        {
            Console.WriteLine($"DEBUG HTTP LibSQL: Parameters: {string.Join(", ", parameters)}");
        }

        // Use v2/pipeline endpoint
        var executeUrl = _baseUrl.TrimEnd('/') + "/v2/pipeline";
        Console.WriteLine($"DEBUG HTTP LibSQL: POST to: {executeUrl}");

        var response = await _httpClient.PostAsync(executeUrl, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"DEBUG HTTP LibSQL: Error response: {response.StatusCode}");
            Console.WriteLine($"DEBUG HTTP LibSQL: Error content: {errorContent}");
            throw new InvalidOperationException($"LibSQL HTTP request failed: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"DEBUG HTTP LibSQL: Response: {responseJson}");
        
        var result = JsonSerializer.Deserialize<LibSqlResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Extract the first execute result
        var executeResult = result?.Results?.FirstOrDefault(r => r.Type == "ok" && r.Response?.Type == "execute");
        return new LibSqlResult(executeResult?.Response?.Result);
    }

    private static LibSqlArg[]? ConvertParametersToArgs(object[]? parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return null;

        return parameters.Select(ConvertParameterToArg).ToArray();
    }

    private static LibSqlArg ConvertParameterToArg(object? parameter)
    {
        return parameter switch
        {
            null => new LibSqlArg { Type = "null", Value = null },
            int i => new LibSqlArg { Type = "integer", Value = i.ToString() },
            long l => new LibSqlArg { Type = "integer", Value = l.ToString() },
            float f => new LibSqlArg { Type = "float", Value = f.ToString() },
            double d => new LibSqlArg { Type = "float", Value = d.ToString() },
            decimal dec => new LibSqlArg { Type = "float", Value = dec.ToString() },
            bool b => new LibSqlArg { Type = "integer", Value = b ? "1" : "0" },
            string s => new LibSqlArg { Type = "text", Value = s },
            byte[] bytes => new LibSqlArg { Type = "blob", Base64 = Convert.ToBase64String(bytes) },
            _ => new LibSqlArg { Type = "text", Value = parameter.ToString() ?? "" }
        };
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

// Request/Response DTOs for Turso v2/pipeline API
public class LibSqlRequest
{
    public LibSqlPipelineRequest[] Requests { get; set; } = Array.Empty<LibSqlPipelineRequest>();
}

public class LibSqlPipelineRequest
{
    public string Type { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LibSqlStatement? Stmt { get; set; }
}

public class LibSqlStatement
{
    public string Sql { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LibSqlArg[]? Args { get; set; }
}

public class LibSqlArg
{
    public string Type { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Base64 { get; set; } // For blob type
}

public class LibSqlResponse
{
    public LibSqlPipelineResult[] Results { get; set; } = Array.Empty<LibSqlPipelineResult>();
}

public class LibSqlPipelineResult
{
    public string Type { get; set; } = string.Empty;
    public LibSqlResponseData? Response { get; set; }
}

public class LibSqlResponseData
{
    public string Type { get; set; } = string.Empty;
    public LibSqlExecuteResult? Result { get; set; }
}

public class LibSqlExecuteResult
{
    public LibSqlColumn[] Cols { get; set; } = Array.Empty<LibSqlColumn>();
    public LibSqlValue[][] Rows { get; set; } = Array.Empty<LibSqlValue[]>();
    public int AffectedRowCount { get; set; }
    public long? LastInsertRowid { get; set; }
    public string? ReplicationIndex { get; set; }
    public int RowsRead { get; set; }
    public int RowsWritten { get; set; }
    public double QueryDurationMs { get; set; }
}

public class LibSqlColumn
{
    public string Name { get; set; } = string.Empty;
    public string? Decltype { get; set; }
}

public class LibSqlValue
{
    public string Type { get; set; } = string.Empty;
    public string? Value { get; set; }
}

// Wrapper to match the interface expected by your current code
public class LibSqlResult
{
    private readonly LibSqlExecuteResult? _data;

    public LibSqlResult(LibSqlExecuteResult? data)
    {
        _data = data;
    }

    public string[] Columns => _data?.Cols?.Select(c => c.Name).ToArray() ?? Array.Empty<string>();
    
    public IEnumerable<object[]> Rows => _data?.Rows?.Select(ConvertRow) ?? Array.Empty<object[]>();
    
    public int RowsAffected => _data?.AffectedRowCount ?? 0;
    
    public long? LastInsertRowid => _data?.LastInsertRowid;

    private static object[] ConvertRow(LibSqlValue[] libSqlRow)
    {
        return libSqlRow.Select(ConvertValue).ToArray();
    }

    private static object ConvertValue(LibSqlValue libSqlValue)
    {
        if (libSqlValue.Value == null)
            return DBNull.Value;

        return libSqlValue.Type switch
        {
            "null" => DBNull.Value,
            "integer" => long.TryParse(libSqlValue.Value, out var longVal) ? longVal : libSqlValue.Value,
            "float" => double.TryParse(libSqlValue.Value, out var doubleVal) ? doubleVal : libSqlValue.Value,
            "text" => libSqlValue.Value,
            "blob" => Convert.FromBase64String(libSqlValue.Value),
            _ => libSqlValue.Value
        };
    }
}