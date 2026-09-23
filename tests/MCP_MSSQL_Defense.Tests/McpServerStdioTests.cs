using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace MCP_MSSQL_Defense.Tests;

/// <summary>
/// 以子處理序啟動實際的 MCP Server，透過 stdio 走完整的 MCP 協定，
/// 與 Claude Code 啟動並連線 Server 的方式相同。
/// </summary>
public sealed class McpServerStdioTests
{
    [Fact]
    public async Task Initialize_ReportsServerNameAndVersion()
    {
        await using var client = await ConnectAsync();

        Assert.Equal("MCP_MSSQL_Defense", client.ServerInfo.Name);
        Assert.Equal(ServerMetadata.Version, client.ServerInfo.Version);
    }

    [Fact]
    public async Task ListTools_ExposesOnlyTestTools()
    {
        await using var client = await ConnectAsync();

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "GetServerInfo", "Ping" }, tools.Select(t => t.Name).Order());
    }

    [Fact]
    public async Task CallPing_ReturnsRunningMessage()
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync("Ping", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(true, result.IsError);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        Assert.Equal("MCP_MSSQL_Defense is running.", text.Text);
    }

    [Fact]
    public async Task CallGetServerInfo_ReturnsNameAndVersion()
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync("GetServerInfo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(true, result.IsError);
        Assert.NotNull(result.StructuredContent);
        var info = result.StructuredContent.Value;
        Assert.Equal("MCP_MSSQL_Defense", info.GetProperty("name").GetString());
        Assert.Equal(ServerMetadata.Version, info.GetProperty("version").GetString());
    }

    private static async Task<McpClient> ConnectAsync()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "MCP_MSSQL_Defense",
            Command = "dotnet",
            Arguments = [Path.Combine(AppContext.BaseDirectory, "MCP_MSSQL_Defense.dll")],
            // SDK 的 Client 在 Dispose 時，會先等待 Server 自行結束（最長 ShutdownTimeout）才關閉 stdin 並終止它，
            // 預設 5 秒會讓每個測試都白等 5 秒，因此縮短。
            ShutdownTimeout = TimeSpan.FromMilliseconds(500),
        });

        return await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
    }
}
