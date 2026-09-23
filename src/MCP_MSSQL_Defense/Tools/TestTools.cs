using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MCP_MSSQL_Defense.Tools;

/// <summary>
/// 連線測試用的 Tools，用來確認 MCP Client（例如 Claude Code）能正常啟動 Server 並呼叫 Tool。
/// 不存取任何資料庫。
/// </summary>
/// <remarks>
/// SDK 預設會把方法名稱轉成 snake_case（例如 get_server_info），因此 Tool 名稱一律以 Name 明確指定。
/// </remarks>
[McpServerToolType]
public sealed class TestTools
{
    [McpServerTool(Name = "Ping", ReadOnly = true, OpenWorld = false)]
    [Description("確認 MCP_MSSQL_Defense Server 是否正常運作。")]
    public static string Ping() => "MCP_MSSQL_Defense is running.";

    [McpServerTool(Name = "GetServerInfo", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("取得 MCP_MSSQL_Defense Server 的名稱與版本。")]
    public static ServerInfoResult GetServerInfo() => new(ServerMetadata.Name, ServerMetadata.Version);
}

public sealed record ServerInfoResult(string Name, string Version);
