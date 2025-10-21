using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace DevOpsExtension.Models;

public class AzureDevOpsExtensionIdentifiers
{
    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("Publisher name (e.g. 'ms' or 'fabrikam')", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string PublisherName { get; set; }

    [TypeProperty("Extension name (e.g. 'vss-code-search')", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string ExtensionName { get; set; }
}

[BicepDocHeading("AzureDevOpsExtension", "Installs an Azure DevOps extension from the marketplace into an organization.")]
[BicepDocExample(
    "Installing a specific extension version",
    "This example shows how to install a specific version of an extension into your Azure DevOps organization.",
    @"resource extension 'AzureDevOpsExtension' = {
  organization: 'myorg'
  publisherName: 'ms'
  extensionName: 'vss-code-search'
  version: '20.263.0.848933653'
}
"
)]
[BicepDocExample(
    "Installing multiple extensions",
    "This example shows how to install multiple extensions using a loop.",
    @"var extensions = [
  {
    publisher: 'ms'
    name: 'vss-code-search'
    version: '20.263.0.848933653'
  }
  {
    publisher: 'SonarSource'
    name: 'sonarqube'
    version: '7.4.1'
  }
]

resource marketplaceExtensions 'AzureDevOpsExtension' = [for ext in extensions: {
  organization: 'myorg'
  publisherName: ext.publisher
  extensionName: ext.name
  version: ext.version
}]
"
)]
[BicepDocExample(
    "Installing an extension with outputs",
    "This example shows how to install an extension and reference its outputs.",
    @"resource extension 'AzureDevOpsExtension' = {
  organization: 'myorg'
  publisherName: 'SonarSource'
  extensionName: 'sonarqube'
  version: '7.4.1'
}

output extensionId string = extension.extensionId
output publisherId string = extension.publisherId
"
)]
[BicepDocCustom("Notes", @"When working with the 'AzureDevOpsExtension' resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
extension azureDevOpsExtension with {
  // extension configuration
}
```

Please note the following important considerations when using the `AzureDevOpsExtension` resource:

- Extensions are installed at the organization level and affect all projects within the organization
- The version must be specified exactly as it appears in the marketplace
- When upgrading an extension, specify the new version number in the `version` property
- If the same version is already installed, no action is taken (idempotent)
- Ensure you have appropriate permissions (Project Collection Administrator role or custom assignment with the 'manager' role)
- Extension names and publisher names are case-sensitive")]
[BicepDocCustom("Required Scopes/Roles", @"To install extensions, the authenticated user or service principal must have one of the following:

- **Project Collection Administrator** role
- A custom assignment with the **manager** role for extension management

The required OAuth scope is: `vso.extension_manage`")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Azure DevOps Extensions Marketplace][00]
- [Extension Management REST API][01]
- [Install extensions for Azure DevOps][02]

<!-- Link reference definitions -->
[00]: https://marketplace.visualstudio.com/azuredevops
[01]: https://learn.microsoft.com/en-us/rest/api/azure/devops/extensionmanagement/installed-extensions?view=azure-devops-rest-7.1
[02]: https://learn.microsoft.com/en-us/azure/devops/marketplace/install-extension")]
[ResourceType("AzureDevOpsExtension")]
public class AzureDevOpsExtension : AzureDevOpsExtensionIdentifiers
{
    [TypeProperty("Version of the extension to install (e.g. '1.0.0')", ObjectTypePropertyFlags.Required)]
    public required string Version { get; set; }

    // Outputs
    [TypeProperty("[OUTPUT] Extension ID", ObjectTypePropertyFlags.ReadOnly)]
    public string? ExtensionId { get; set; }

    [TypeProperty("[OUTPUT] Publisher ID", ObjectTypePropertyFlags.ReadOnly)]
    public string? PublisherId { get; set; }
}
