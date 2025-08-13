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
    public string? TenantId { get; set; }

    [TypeProperty("ClientId (application id) of the user-assigned managed identity used for Workload Identity Federation.", ObjectTypePropertyFlags.Required)]
    public string? ClientId { get; set; }

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
