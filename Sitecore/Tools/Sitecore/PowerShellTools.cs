using ModelContextProtocol.Server;
using System.ComponentModel;

internal sealed class PowerShellTools
{
    [McpServerTool]
    [Description("Executes a Sitecore PowerShell script via Sitecore PowerShell Extensions (SPE) remoting and returns the output. Use this to run any PowerShell script against Sitecore — query items, manage users, publish, index, or perform administrative tasks.")]
    public async Task<string> RunSitecorePowerShellScript(
        [Description("The PowerShell script to execute. For example: Get-Item -Path 'master:/sitecore/content/Home' | Select-Object Name, ID, TemplateName | ConvertTo-Json")] string script)
    {
        var client = new SitecorePowerShellClient(SitecoreSettings.LoadFromEnvironment().PowerShell);
        return await client.ExecuteScriptAsync(script);
    }
}
