using System.Reflection;

namespace MCP_MSSQL_Defense;

/// <summary>
/// MCP Server 的識別資訊。版本號統一由 .csproj 的 &lt;Version&gt; 管理。
/// </summary>
public static class ServerMetadata
{
    public const string Name = "MCP_MSSQL_Defense";

    public static string Version { get; } =
        typeof(ServerMetadata).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.0.0";
}
