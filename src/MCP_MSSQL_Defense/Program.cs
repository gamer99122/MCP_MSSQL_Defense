using MCP_MSSQL_Defense;
using MCP_MSSQL_Defense.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    // AI CLI 啟動 Server 時的工作目錄是使用者的專案目錄；以程式所在目錄作為 ContentRoot，
    // 才不會讀到該專案目錄底下的 appsettings.json。
    ContentRootPath = AppContext.BaseDirectory,
});

// stdio transport 以 stdout 傳送 MCP JSON-RPC 訊息，任何其他寫入 stdout 的內容都會破壞協定，
// 因此 log 一律導向 stderr，程式中也不可使用 Console.WriteLine。
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = ServerMetadata.Name,
            Version = ServerMetadata.Version,
        };
    })
    .WithStdioServerTransport()
    // Tool 採明確註冊：只有在這裡註冊的類別才會公開給 AI 呼叫。
    .WithTools<TestTools>();

await builder.Build().RunAsync();
