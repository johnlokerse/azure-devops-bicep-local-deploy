using Bicep.Local.Extension.Types.Attributes;

namespace DevOpsExtension.Models;

public class Configuration
{
    [TypeProperty("Personal Access Token (PAT) for Azure DevOps with appropriate scopes. If omitted, environment variable AZDO_PAT is used.")]
    public string? AccessToken { get; set; }
}
