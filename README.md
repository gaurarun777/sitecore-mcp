# Sitecore MCP Server

A minimal [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server for Sitecore, built with the [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk). It exposes focused tools for GraphQL, Item Service, PowerShell, and CSD role access management — giving AI assistants (GitHub Copilot Chat, Claude, etc.) direct access to your Sitecore instance.

## Tools

### Core Tools

| Tool | Area | Description |
|---|---|---|
| `QuerySitecoreGraphQl` | GraphQL | Run any GraphQL query against a Sitecore schema |
| `GetSitecoreItemByPath` | Item Service | Retrieve a Sitecore item and its fields by content tree path |
| `RunSitecorePowerShellScript` | PowerShell | Execute any script via Sitecore PowerShell Extensions (SPE) remoting |

### CSD Role Access Management

| Tool | Description |
|---|---|
| `SearchCsdRole` | Search Sitecore roles across all domains by partial name match |
| `GrantCsdAccess` | Grant CSD access (Read + Write with Inheritance) to roles on content paths |
| `GrantCsdAdminAccess` | Grant CSD Admin access (Read, Write, Rename, Create, Delete, Administer with Inheritance) to roles on content paths |
| `RevokeCsdAccess` | Remove CSD Allow rules (Read + Write) from roles on content paths — never adds Deny |
| `RevokeCsdAdminAccess` | Remove CSD Admin Allow rules (all 6 rights) from roles on content paths — never adds Deny |

#### CSD Access Rights Reference

| Access Type | Rights Granted / Revoked | Propagation |
|---|---|---|
| CSD | Read, Write | Any (item + all descendants) |
| CSD Admin | Read, Write, Rename, Create, Delete, Administer | Any (item + all descendants) |

---

## Prerequisites

- .NET 8 SDK
- A running Sitecore XP/XM CM instance with:
  - GraphQL endpoint enabled (e.g. `/sitecore/api/graph/edge`)
  - Sitecore Item Service (SSC) enabled
  - [Sitecore PowerShell Extensions (SPE)](https://doc.sitecorepowershell.com/) installed and remoting enabled

---

## Getting Started

### 1. Clone and open

```bash
cd "C:\mcp\sample mcp\sitecoremcp"
```

### 2. Configure environment variables

The server reads all configuration from environment variables at tool call time. Set these before running, or use the VS Code MCP inputs (see below).

| Variable | Required | Description |
|---|---|---|
| `GRAPHQL_ENDPOINT` | Yes | Full GraphQL endpoint URL, e.g. `https://cm.dev.local/sitecore/api/graph/edge` |
| `GRAPHQL_API_KEY` | Yes | Sitecore GraphQL API key GUID |
| `GRAPHQL_SCHEMAS` | No | Comma-separated schemas, default `edge` |
| `ITEM_SERVICE_SERVER_URL` | Yes* | Sitecore CM base URL, e.g. `https://cm.dev.local` |
| `ITEM_SERVICE_USERNAME` | Yes* | Sitecore username |
| `ITEM_SERVICE_PASSWORD` | Yes* | Sitecore password |
| `ITEM_SERVICE_DOMAIN` | No | Domain, default `sitecore` |
| `POWERSHELL_SERVER_URL` | Yes* | Sitecore CM base URL |
| `POWERSHELL_USERNAME` | Yes* | Sitecore username |
| `POWERSHELL_PASSWORD` | Yes* | Sitecore password |
| `POWERSHELL_DOMAIN` | No | Domain, default `sitecore` |
| `SITECORE_ALLOW_INVALID_CERTIFICATES` | No | Set `true` to bypass TLS validation (dev only) |

*Required only when using the corresponding tool.

### 3. Run with VS Code

The `.vscode/mcp.json` file configures the server for VS Code's MCP client. Open the project in VS Code — it will prompt you for all credentials interactively when the server starts.

### 4. Run manually

```bash
dotnet run --project SitecoreMcpServer.csproj
```

---

## Sample Prompts

### GraphQL — `QuerySitecoreGraphQl`

**Get a content item by path:**
```
Query the Sitecore Home item via GraphQL using the path /sitecore/content/Home in English.
```

**Fetch specific fields:**
```
Use Sitecore GraphQL to get the Title and Body fields from /sitecore/content/Home/About-Us.
```

**Explore the schema:**
```
Run a Sitecore GraphQL introspection query to list all available types on the edge schema.
Use this query: query { __schema { types { name kind } } }
```

**Query with variables:**
```
Run this GraphQL query against the master schema with variables {"path": "/sitecore/content/Home"}:
query GetItem($path: String!) { item(path: $path, language: "en") { id name path } }
```

**List children of a node:**
```
Use GraphQL to query all direct children of /sitecore/content and return their id, name, and path.
```

---

### Item Service — `GetSitecoreItemByPath`

**Get the Home item:**
```
Get the Sitecore item at /sitecore/content/Home from the master database.
```

**Get an item in a specific language:**
```
Retrieve the Sitecore item at /sitecore/content/Home/News using Item Service with language fr-FR.
```

**Inspect site root:**
```
Use Item Service to get the item at /sitecore/content and show me all its fields.
```

**Check an item in the web database:**
```
Get the Sitecore item /sitecore/content/Home from the web database using Item Service
and tell me if it has been published (check the __Updated field).
```

**Get a template definition:**
```
Use Item Service to retrieve the item at /sitecore/templates/Sample/Sample Item
so I can see its template fields and structure.
```

---

### PowerShell — `RunSitecorePowerShellScript`

**List children of a node:**
```
Run a Sitecore PowerShell script to get all direct children of /sitecore/content/Home
and return their Name, ID, and TemplateName as JSON.
```
> Script: `Get-ChildItem -Path 'master:/sitecore/content/Home' | Select-Object Name, ID, TemplateName | ConvertTo-Json`

**Find items by template:**
```
Write and run a Sitecore PowerShell script to find all items under /sitecore/content
that use the template named "Article Page" and return their paths.
```
> Script: `Get-ChildItem -Path 'master:/sitecore/content' -Recurse | Where-Object { $_.TemplateName -eq 'Article Page' } | Select-Object Name, Paths.FullPath | ConvertTo-Json`

**Check item fields:**
```
Run a Sitecore PowerShell script to get all field values for the item at
/sitecore/content/Home and return them as JSON.
```
> Script: `Get-Item -Path 'master:/sitecore/content/Home' | Get-ItemField -IncludeStandardFields | Select-Object Name, Value | ConvertTo-Json`

**Publish an item:**
```
Run a Sitecore PowerShell script to publish the item at /sitecore/content/Home
from master to web for the en language.
```
> Script: `Publish-Item -Path 'master:/sitecore/content/Home' -Target 'web' -Language 'en' -Recurse`

**List Sitecore users:**
```
Run a PowerShell script against Sitecore to list all users in the sitecore domain
and return their names and email addresses as JSON.
```
> Script: `Get-User -Filter 'sitecore\*' | Select-Object Name, Email | ConvertTo-Json`

**Clear Sitecore caches:**
```
Run a Sitecore PowerShell script to clear all Sitecore caches on the CM server.
```
> Script: `Clear-SitecoreCache`

---

### CSD Role Access Management

#### Typical workflow

1. **Search** for the exact role identity using a partial name.
2. **Verify** the paths exist.
3. **Grant or Revoke** access for the resolved role(s).

---

**Search for a role by partial name — `SearchCsdRole`**

```
Search for Sitecore roles matching the name "cgdev".
```

```
Find all Sitecore roles that contain "developer" in their name across all domains.
```

> The tool returns a list of matching role identities (e.g. `sitecore\cgdev`, `cog\cgdeveloper`) that you can pass directly to the grant/revoke tools.

---

**Grant CSD access to a role — `GrantCsdAccess`**

```
Grant CSD access for the role "sitecore\cgdev" on the following paths:
- /sitecore/content/DemoSite
- /sitecore/media library/DemoSite
```

```
Assign CSD access (Read and Write) to roles ["sitecore\cgdev", "cog\cgdev"] on
/sitecore/content/SampleSite and /sitecore/media library/SampleSite.
```

> Paths that do not exist in Sitecore are reported as `PathNotFound` and skipped — the tool does not fail. All rules are applied with `PropagationType Any` so they inherit to child items.

---

**Grant CSD Admin access — `GrantCsdAdminAccess`**

```
Grant CSD Admin access to the role "sitecore\cgadmin" on /sitecore/content/DemoSite
and /sitecore/media library/DemoSite.
```

```
Assign full CSD Admin permissions (Read, Write, Rename, Create, Delete, Administer)
to ["sitecore\cgadmin"] on /sitecore/content/SampleSite.
```

---

**Revoke CSD access — `RevokeCsdAccess`**

```
Revoke CSD access for "sitecore\cgdev" on /sitecore/content/DemoSite
and /sitecore/media library/DemoSite.
```

```
Remove CSD Read and Write permissions for roles ["sitecore\cgdev", "cog\cgdev"]
from /sitecore/content/SampleSite. Do not add any Deny rules.
```

> Only existing `Allow` rules are removed. No `Deny` rules are ever added. If the role had no rules on an item, `RulesRevoked` will be `0` for that path.

---

**Revoke CSD Admin access — `RevokeCsdAdminAccess`**

```
Revoke all CSD Admin permissions for "sitecore\cgadmin" on /sitecore/content/DemoSite.
```

```
Remove CSD Admin access (Read, Write, Rename, Create, Delete, Administer) for
["sitecore\cgadmin"] on /sitecore/content/SampleSite and /sitecore/media library/SampleSite.
```

---

**End-to-end example prompt:**

```
I need to set up CSD access for the "cgdev" team on the DemoSite.

1. Search for Sitecore roles matching "cgdev" to find the exact role names.
2. Grant CSD access (Read and Write) for the found roles on:
   - /sitecore/content/DemoSite
   - /sitecore/media library/DemoSite
3. Also grant CSD Admin access to any role matching "cgadmin" on the same paths.
```

---

## Project Structure

```
sitecoremcp/
├── SitecoreMcpServer.csproj
├── Program.cs
├── README.md
├── .mcp/
│   └── server.json                        # MCP server manifest
├── .vscode/
│   └── mcp.json                           # VS Code MCP launch config
├── Sitecore/
│   ├── Configuration/
│   │   └── SitecoreSettings.cs            # Environment variable config
│   ├── Infrastructure/
│   │   ├── SitecoreHttpClientFactory.cs   # HttpClient with optional TLS bypass
│   │   └── ToolJson.cs                    # JSON serialization helpers
│   ├── GraphQl/
│   │   └── SitecoreGraphQlClient.cs       # GraphQL HTTP client
│   ├── ItemService/
│   │   └── SitecoreItemServiceClient.cs   # Item Service REST client (cookie auth)
│   └── PowerShell/
│       └── SitecorePowerShellClient.cs    # SPE remoting HTTP client
└── Tools/
    └── Sitecore/
        ├── GraphQlTools.cs                # MCP tool: QuerySitecoreGraphQl
        ├── ItemServiceTools.cs            # MCP tool: GetSitecoreItemByPath
        ├── PowerShellTools.cs             # MCP tool: RunSitecorePowerShellScript
        └── CsdAccessTools.cs             # MCP tools: SearchCsdRole, GrantCsdAccess,
                                           #   GrantCsdAdminAccess, RevokeCsdAccess,
                                           #   RevokeCsdAdminAccess
```

---

## How It Works

1. **Program.cs** registers all tool classes with the MCP SDK using the `stdio` transport.
2. Each tool method is decorated with `[McpServerTool]` — the MCP SDK discovers and exposes these automatically.
3. Tools load configuration from environment variables at call time via `SitecoreSettings.LoadFromEnvironment()`.
4. **GraphQL** queries are POSTed with the `sc_apikey` header to the configured endpoint.
5. **Item Service** authenticates via a cookie-based login (`/sitecore/api/ssc/auth/login`) and issues GET requests.
6. **PowerShell** sends scripts to the SPE remoting endpoint (`/-/script/script/`) using HTTP Basic auth.
7. **CSD Access tools** build inline SPE scripts at call time and execute them through the same PowerShell remoting client. Role searches use `Get-Role -Filter`, access grants use `New-ItemAcl` + `Add-ItemAcl` with `PropagationType Any`, and revokes read the item's explicit ACL, filter out the target `Allow` entries, and reset with `Set-ItemAcl` — no `Deny` rules are ever written.

---

## License

MIT
