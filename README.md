# MCP_MSSQL_Defense

以 C# / .NET 8 開發的 MCP（Model Context Protocol）Server，定位為 AI CLI 與 MSSQL 之間的安全防護層。
最終目標是阻止 AI 直接對 MSSQL 執行 `INSERT`、`UPDATE`、`DELETE`、`MERGE`、`DROP`、`ALTER`、`TRUNCATE` 等資料異動操作。

> **目前為第一階段**：只提供最小可運作的 MCP Server 與測試用 Tools，用來確認 Claude Code 能正常連線並呼叫 Tool。
> 本階段**不連線任何資料庫**，也不包含任何帳號、密碼或 Connection String。

## 第一階段內容

| Tool | 說明 | 回傳 |
|---|---|---|
| `Ping` | 確認 Server 是否正常運作 | `MCP_MSSQL_Defense is running.` |
| `GetServerInfo` | 取得 Server 名稱與版本 | `{"name":"MCP_MSSQL_Defense","version":"0.1.0"}` |

- Transport：stdio（由 Claude Code 直接啟動 Server 程序）
- MCP SDK：官方 C# SDK [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) 2.2.0
- 兩個 Tool 都標示為唯讀（MCP annotation `readOnlyHint: true`）

## 專案結構

```
MCP_MSSQL_Defense/
├─ src/
│  └─ MCP_MSSQL_Defense/            MCP Server（Console App）
│     ├─ Program.cs                 Host 設定、stdio transport、Tool 註冊
│     ├─ ServerMetadata.cs          Server 名稱與版本
│     └─ Tools/
│        └─ TestTools.cs            Ping、GetServerInfo
├─ tests/
│  └─ MCP_MSSQL_Defense.Tests/      xUnit 整合測試
├─ README.md
├─ .gitignore
└─ MCP_MSSQL_Defense.sln
```

## 環境需求

- .NET SDK 8.0 以上（SDK 9 / 10 也能建置 `net8.0` 專案）
- .NET 8 Runtime

## 建置與執行

以下指令皆在專案根目錄（`MCP_MSSQL_Defense.sln` 所在位置）執行。

### Build

```powershell
dotnet build
```

### Test

```powershell
dotnet test
```

測試會以子程序實際啟動 Server，透過 stdio 走完整的 MCP 流程（`initialize` → `tools/list` → `tools/call`），與 Claude Code 連線的方式相同。

### Run

```powershell
dotnet run --project src/MCP_MSSQL_Defense
```

stdio Server 平常由 MCP Client（Claude Code）啟動，手動執行只用來確認程式能否啟動：
啟動後會等待 stdin 傳入 MCP JSON-RPC 訊息，stdout 不會有輸出，log 會顯示在 stderr。按 `Ctrl+C` 結束。

### Publish

```powershell
dotnet publish src/MCP_MSSQL_Defense -c Release -o publish
```

輸出位於 `publish/`（已排除於版控），其中 `MCP_MSSQL_Defense.exe` 可直接執行（需已安裝 .NET 8 Runtime）。

## 加入 Claude Code

以下的 `<專案路徑>` 請換成本專案的絕對路徑；路徑含空白時請保留雙引號。

### 方式一：使用 publish 後的執行檔（建議）

先執行上面的 Publish，再執行：

```powershell
claude mcp add mssql-defense -- "<專案路徑>\publish\MCP_MSSQL_Defense.exe"
```

啟動快、行為固定。修改程式後需重新 publish；Server 執行中時 Windows 會鎖住執行檔，請先結束使用中的 Claude Code 再 publish。

### 方式二：開發期間直接從原始碼執行

```powershell
claude mcp add mssql-defense -- dotnet run --project "<專案路徑>\src\MCP_MSSQL_Defense"
```

每次啟動都會先建置，速度較慢，但修改程式後不需重新 publish。

### 設定範圍（scope）

`claude mcp add` 預設為 `local`，可用 `-s` 指定：

| scope | 說明 |
|---|---|
| `local`（預設） | 只在目前專案中可用，設定僅限自己 |
| `project` | 寫入專案根目錄的 `.mcp.json`，可提交版控與團隊共用 |
| `user` | 自己的所有專案都可用 |

例如：`claude mcp add -s user mssql-defense -- "<專案路徑>\publish\MCP_MSSQL_Defense.exe"`

### 使用 .mcp.json

也可以在要使用此 Server 的專案根目錄手動建立 `.mcp.json`（JSON 字串中的 `\` 要寫成 `\\`）：

```json
{
  "mcpServers": {
    "mssql-defense": {
      "type": "stdio",
      "command": "C:\\path\\to\\MCP_MSSQL_Defense\\publish\\MCP_MSSQL_Defense.exe",
      "args": []
    }
  }
}
```

專案層級的 `.mcp.json` 第一次使用時，Claude Code 會詢問是否核准。

### 確認連線

```powershell
claude mcp list
```

`mssql-defense` 的狀態顯示為 Connected 即表示連線成功。也可以在 Claude Code 中輸入 `/mcp` 查看 Server 狀態與 Tool 清單，
再直接請 Claude 呼叫，例如：「請呼叫 mssql-defense 的 Ping 與 GetServerInfo」。

在 Claude Code 中（例如權限設定），Tool 的完整名稱為 `mcp__mssql-defense__Ping` 與 `mcp__mssql-defense__GetServerInfo`。

## 開發注意事項

- **stdout 保留給 MCP 協定**：stdio transport 以 stdout 傳送 JSON-RPC 訊息，程式中不可使用 `Console.WriteLine`，否則會破壞協定。log 一律透過 `ILogger`（已設定輸出到 stderr）。
- **Tool 名稱要明確指定**：SDK 預設會把方法名稱轉成 snake_case（例如 `GetServerInfo` → `get_server_info`），因此以 `[McpServerTool(Name = "...")]` 指定名稱。
- **Tool 採明確註冊**：只有在 `Program.cs` 以 `.WithTools<T>()` 註冊的類別才會公開給 AI，不使用整個組件自動掃描，避免意外公開 Tool。
- **版本號**：只需修改 `src/MCP_MSSQL_Defense/MCP_MSSQL_Defense.csproj` 的 `<Version>`，`GetServerInfo` 與 MCP handshake 回報的版本會同步。
- **設定檔位置**：Server 以程式所在目錄作為 ContentRoot，不會讀取 Claude Code 啟動時工作目錄下的 `appsettings.json`。

## 後續擴充規劃

以下為後續階段預計加入的內容，**本階段尚未實作**：

```
src/MCP_MSSQL_Defense/
├─ Tools/
│  ├─ TestTools.cs        第一階段（已完成）
│  └─ MssqlTools.cs       對 AI 公開的 MSSQL 查詢 Tool
└─ Security/
   ├─ SqlValidator.cs     分析 SQL，阻擋 INSERT / UPDATE / DELETE / MERGE / DROP / ALTER / TRUNCATE 等異動
   └─ SecurityPolicy.cs   防護規則設定
```

擴充方式：

1. `SqlValidator`、`SecurityPolicy` 在 `Program.cs` 註冊到 DI（`builder.Services.AddSingleton<...>()`）。
2. `MssqlTools` 使用實例方法，透過建構子注入上述服務（SDK 每次呼叫 Tool 都會透過 DI 建立新實例），再以 `.WithTools<MssqlTools>()` 註冊。
3. 對應的測試放在 `tests/`。

正式使用時的資安建議：

- 連線字串與帳密不要進版控，改以環境變數提供（`claude mcp add -e KEY=value ...` 或 `.mcp.json` 的 `env`）。
- 以 publish 後的執行檔啟動，並放在 AI 沒有寫入權限的位置。若以 `dotnet run` 從原始碼啟動，AI 只要修改原始碼，下次啟動就會套用被修改的防護邏輯。
- 資料庫帳號本身也只授予唯讀權限（例如 `db_datareader`），讓這個 MCP Server 不是唯一的防線。
