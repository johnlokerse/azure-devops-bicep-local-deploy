using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace DevOpsExtension.Models;

public class AzureDevOpsServiceConnectionIdentifiers
{
    [TypeProperty("Service connection name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("Project name that will contain the service connection", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Project { get; set; }
}

[BicepDocHeading("AzureDevOpsServiceConnection", "Represents an Azure service connection for authenticating Azure DevOps pipelines with Azure resources using Workload Identity Federation.")]
[BicepDocExample(
    "Creating a subscription-scoped service connection",
    "This example shows how to create a service connection for accessing Azure resources at the subscription level.",
    @"resource serviceConnection 'AzureDevOpsServiceConnection' = {
  name: 'azure-prod'
  organization: 'myorg'
  project: 'my-project'
  scopeLevel: 'Subscription'
  subscriptionId: '12345678-1234-1234-1234-123456789012'
  subscriptionName: 'Production Subscription'
  tenantId: '87654321-4321-4321-4321-210987654321'
  clientId: '11111111-2222-3333-4444-555555555555'
  description: 'Service connection for production Azure resources'
  grantAllPipelines: false
}
"
)]
[BicepDocExample(
    "Creating a management group-scoped service connection",
    "This example shows how to create a service connection for accessing Azure resources at the management group level.",
    @"resource mgServiceConnection 'AzureDevOpsServiceConnection' = {
  name: 'azure-enterprise'
  organization: 'myorg'
  project: 'platform-team'
  scopeLevel: 'ManagementGroup'
  managementGroupName: 'Enterprise'
  managementGroupId: 'mg-enterprise-001'
  tenantId: '87654321-4321-4321-4321-210987654321'
  clientId: '11111111-2222-3333-4444-555555555555'
  description: 'Service connection for enterprise-wide Azure governance'
  grantAllPipelines: true
}
"
)]
[BicepDocExample(
    "Creating service connections for multiple environments",
    "This example shows how to create service connections for different environments using loops.",
    @"var environments = [
  {
    name: 'dev'
    subscriptionId: '11111111-1111-1111-1111-111111111111'
    subscriptionName: 'Development'
  }
  {
    name: 'staging'
    subscriptionId: '22222222-2222-2222-2222-222222222222'
    subscriptionName: 'Staging'
  }
  {
    name: 'prod'
    subscriptionId: '33333333-3333-3333-3333-333333333333'
    subscriptionName: 'Production'
  }
]

resource envServiceConnections 'AzureDevOpsServiceConnection' = [for env in environments: {
  name: 'azure-${env.name}'
  organization: 'myorg'
  project: 'multi-env-app'
  scopeLevel: 'Subscription'
  subscriptionId: env.subscriptionId
  subscriptionName: env.subscriptionName
  tenantId: '87654321-4321-4321-4321-210987654321'
  clientId: '11111111-2222-3333-4444-555555555555'
  description: 'Service connection for ${env.name} environment'
  grantAllPipelines: env.name == 'dev' ? true : false
}]
"
)]
[BicepDocCustom("Notes", @"When working with the 'AzureDevOpsServiceConnection' resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
extension azureDevOpsExtension with {
  // extension configuration
}
```

Please note the following important considerations when using the `AzureDevOpsServiceConnection` resource:

- This resource creates service connections using Workload Identity Federation for secure, passwordless authentication
- For subscription-scoped connections: `subscriptionId` and `subscriptionName` are required
- For management group-scoped connections: `managementGroupName` and `managementGroupId` are required
- The `clientId` should be the Application (client) ID of a user-assigned managed identity
- The `tenantId` is the Azure AD tenant containing the managed identity
- Set `grantAllPipelines: true` to automatically authorize all pipelines (use with caution)
- Service connection IDs and federation details are available as output properties")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Azure service connections in Azure DevOps][00]
- [Workload Identity Federation][01]
- [Configure service connections for Azure Resource Manager][02]

<!-- Link reference definitions -->
[00]: https://docs.microsoft.com/en-us/azure/devops/pipelines/library/service-endpoints
[01]: https://docs.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation
[02]: https://docs.microsoft.com/en-us/azure/devops/pipelines/library/connect-to-azure")]
[ResourceType("AzureDevOpsServiceConnection")]
public class AzureDevOpsServiceConnection : AzureDevOpsServiceConnectionIdentifiers
{
    public enum ServiceConnectionScopeLevel
    {
        Subscription,
        ManagementGroup
    }

    [TypeProperty("Scope level: Subscription (default) or ManagementGroup.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ServiceConnectionScopeLevel? ScopeLevel { get; set; } = ServiceConnectionScopeLevel.Subscription;

    [TypeProperty("Azure subscription id (GUID). Required when scopeLevel = Subscription.")]
    public string? SubscriptionId { get; set; }

    [TypeProperty("Azure subscription display name. Required when scopeLevel = Subscription.")]
    public string? SubscriptionName { get; set; }

    [TypeProperty("Azure AD tenant id (directory id) containing the user-assigned managed identity.", ObjectTypePropertyFlags.Required)]
    public required string TenantId { get; set; }

    [TypeProperty("ClientId (application id) of the user-assigned managed identity used for Workload Identity Federation.", ObjectTypePropertyFlags.Required)]
    public required string ClientId { get; set; }

    [TypeProperty("Management group name. Required when scopeLevel = ManagementGroup.")]
    public string? ManagementGroupName { get; set; }

    [TypeProperty("Management group id. Required when scopeLevel = ManagementGroup.")]
    public string? ManagementGroupId { get; set; }

    [TypeProperty("Description for the service connection")]
    public string? Description { get; set; }

    [TypeProperty("Grant access permission to all pipelines (authorizes all pipelines after creation)")]
    public bool GrantAllPipelines { get; set; }

    // Outputs
    [TypeProperty("[OUTPUT] Authorization scheme actually used")]
    public string? AuthorizationScheme { get; set; }

    [TypeProperty("[OUTPUT]Service connection id (GUID)")]
    public string? ServiceConnectionId { get; set; }

    [TypeProperty("[OUTPUT] Service connection URL")]
    public string? Url { get; set; }

    [TypeProperty("[OUTPUT] Workload Identity Federation Issuer")]
    public string? Issuer { get; set; }

    [TypeProperty("[OUTPUT] Workload Identity Federation Subject Identifier")]
    public string? SubjectIdentifier { get; set; }
}
