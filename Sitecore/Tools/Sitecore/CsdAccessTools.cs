using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

internal sealed class CsdAccessTools
{
    private static readonly string[] CsdRights = ["item:read", "item:write"];
    private static readonly string[] CsdAdminRights = ["item:read", "item:write", "item:rename", "item:create", "item:delete", "item:admin"];

    [McpServerTool]
    [Description("Searches Sitecore roles across all domains by partial name match. Returns a list of matching role identities. Use this first to find the exact role identity before calling grant or revoke tools. For example, searching 'cgdev' might return 'sitecore\\cgdev', 'cog\\cgdev', or 'sitecore\\cgdeveloper'.")]
    public async Task<string> SearchCsdRole(
        [Description("Partial role name to search for (wildcard match, case-insensitive). For example: 'cgdev', 'developer', 'cdadmin'.")] string searchTerm)
    {
        var escaped = searchTerm.Replace("'", "''", StringComparison.Ordinal);
        var script = $$"""
$ErrorActionPreference = 'Stop'
$matchedRoles = @(Get-Role -Filter '*{{escaped}}*')
$matchData = $matchedRoles | ForEach-Object {
    [pscustomobject]@{
        Identity = $_.Name
        Domain   = if ($_.Domain) { $_.Domain.Name } else { $null }
    }
}
[pscustomobject]@{
    SearchTerm = '{{escaped}}'
    MatchCount = $matchedRoles.Count
    Matches    = @($matchData)
} | ConvertTo-Json -Depth 4
""";
        return await ExecuteAsync(script);
    }

    [McpServerTool]
    [Description("Grants CSD access (Read and Write with Inheritance propagated to descendants) to a list of Sitecore roles on the given content paths. Each path is checked for existence first — paths not found are reported but do not cause failure. Use SearchCsdRole to get exact role identities before calling this tool.")]
    public async Task<string> GrantCsdAccess(
        [Description("Fully-qualified Sitecore role names to grant CSD access to. Example: [\"sitecore\\\\cgdev\", \"cog\\\\cgdev\"].")] string[] roles,
        [Description("Sitecore content paths to apply access on. Example: [\"/sitecore/content/DemoSite\", \"/sitecore/media library/DemoSite\"].")] string[] paths)
    {
        return await ExecuteAsync(BuildGrantScript(roles, paths, isCsdAdmin: false));
    }

    [McpServerTool]
    [Description("Grants CSD Admin access (Read, Write, Rename, Create, Delete and Administer with Inheritance propagated to descendants) to a list of Sitecore roles on the given content paths. Each path is checked for existence first. Use SearchCsdRole to get exact role identities before calling this tool.")]
    public async Task<string> GrantCsdAdminAccess(
        [Description("Fully-qualified Sitecore role names to grant CSD Admin access to. Example: [\"sitecore\\\\cgadmin\"].")] string[] roles,
        [Description("Sitecore content paths to apply access on.")] string[] paths)
    {
        return await ExecuteAsync(BuildGrantScript(roles, paths, isCsdAdmin: true));
    }

    [McpServerTool]
    [Description("Revokes CSD access by removing existing Allow rules for Read and Write from a list of Sitecore roles on the given content paths. Does NOT add Deny rules — only removes Allow entries that were previously granted. Paths not found are reported but do not cause failure.")]
    public async Task<string> RevokeCsdAccess(
        [Description("Fully-qualified Sitecore role names to revoke CSD access from.")] string[] roles,
        [Description("Sitecore content paths to revoke access on.")] string[] paths)
    {
        return await ExecuteAsync(BuildRevokeScript(roles, paths, isCsdAdmin: false));
    }

    [McpServerTool]
    [Description("Revokes CSD Admin access by removing existing Allow rules for Read, Write, Rename, Create, Delete and Administer from a list of Sitecore roles on the given content paths. Does NOT add Deny rules — only removes Allow entries. Paths not found are reported but do not cause failure.")]
    public async Task<string> RevokeCsdAdminAccess(
        [Description("Fully-qualified Sitecore role names to revoke CSD Admin access from.")] string[] roles,
        [Description("Sitecore content paths to revoke access on.")] string[] paths)
    {
        return await ExecuteAsync(BuildRevokeScript(roles, paths, isCsdAdmin: true));
    }

    private static async Task<string> ExecuteAsync(string script)
    {
        var client = new SitecorePowerShellClient(SitecoreSettings.LoadFromEnvironment().PowerShell);
        return await client.ExecuteScriptAsync(script);
    }

    private static string BuildGrantScript(string[] roles, string[] paths, bool isCsdAdmin)
    {
        var rights = isCsdAdmin ? CsdAdminRights : CsdRights;
        var accessType = isCsdAdmin ? "CSDAdmin" : "CSD";

        return $$"""
$ErrorActionPreference = 'Stop'
$roles   = {{ToPsArray(roles)}}
$paths   = {{ToPsArray(paths)}}
$rights  = {{ToPsArray(rights)}}
$results = [System.Collections.Generic.List[object]]::new()

foreach ($path in $paths) {
    $item = Get-Item -Path "master:$path" -ErrorAction SilentlyContinue
    if (-not $item) {
        $results.Add([pscustomobject]@{ Path = $path; Status = 'PathNotFound' })
        continue
    }
    $granted = [System.Collections.Generic.List[string]]::new()
    foreach ($roleName in $roles) {
        foreach ($right in $rights) {
            $acl = New-ItemAcl -Identity $roleName -AccessRight $right -PropagationType 'Any' -SecurityPermission 'AllowAccess'
            Add-ItemAcl -Item $item -AccessRules $acl
        }
        $granted.Add($roleName)
    }
    $results.Add([pscustomobject]@{
        Path           = $path
        Status         = 'AccessGranted'
        AccessType     = '{{accessType}}'
        ItemId         = $item.ID.ToString()
        RightsGranted  = $rights
        RolesProcessed = @($granted)
    })
}

[pscustomobject]@{ Results = @($results) } | ConvertTo-Json -Depth 6
""";
    }

    private static string BuildRevokeScript(string[] roles, string[] paths, bool isCsdAdmin)
    {
        var rights = isCsdAdmin ? CsdAdminRights : CsdRights;
        var accessType = isCsdAdmin ? "CSDAdmin" : "CSD";

        return $$"""
$ErrorActionPreference = 'Stop'
$roles          = {{ToPsArray(roles)}}
$paths          = {{ToPsArray(paths)}}
$rightsToRevoke = {{ToPsArray(rights)}}
$results = [System.Collections.Generic.List[object]]::new()

foreach ($path in $paths) {
    $item = Get-Item -Path "master:$path" -ErrorAction SilentlyContinue
    if (-not $item) {
        $results.Add([pscustomobject]@{ Path = $path; Status = 'PathNotFound' })
        continue
    }

    $currentRules = @($item.Security.GetAccessRules())
    $newRules     = New-Object Sitecore.Security.AccessControl.AccessRuleCollection
    $revokedCount = 0

    foreach ($rule in $currentRules) {
        $isTarget = ($roles -contains $rule.Account.Name) -and
                    ($rightsToRevoke -contains $rule.AccessRight.Name) -and
                    ($rule.SecurityPermission.ToString() -eq 'AllowAccess')
        if ($isTarget) {
            $revokedCount++
        } else {
            [void]$newRules.Add($rule)
        }
    }

    # SetAccessRules handles empty collections, while Set-ItemAcl rejects them.
    $item.Security.SetAccessRules($newRules)

    $results.Add([pscustomobject]@{
        Path         = $path
        Status       = 'AccessRevoked'
        AccessType   = '{{accessType}}'
        ItemId       = $item.ID.ToString()
        RulesRevoked = $revokedCount
    })
}

[pscustomobject]@{ Results = @($results) } | ConvertTo-Json -Depth 6
""";
    }

    private static string ToPsArray(string[] values)
    {
        var sb = new StringBuilder("@(");
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append('\'');
            sb.Append(values[i].Replace("'", "''", StringComparison.Ordinal));
            sb.Append('\'');
        }
        sb.Append(')');
        return sb.ToString();
    }
}
