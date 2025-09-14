using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;

public enum CatalogType
{
    MANAGED_CATALOG,
    DELTASHARING_CATALOG,
    FOREIGN_CATALOG,
    SYSTEM_CATALOG
}

public enum IsolationMode
{
    OPEN,
    ISOLATED
}

public enum PredictiveOptimizationFlag
{
    DISABLE,
    ENABLE,
    INHERIT
}

public enum SecurableType
{
    CATALOG,
    SCHEMA,
    TABLE,
    VOLUME
}

public enum ProvisioningState
{
    PROVISIONING,
    PROVISIONED,
    FAILED
}

public enum InheritedFromType
{
    CATALOG,
    SCHEMA,
    TABLE
}


[BicepFrontMatter("category", "Unity Catalog")]
[BicepDocHeading("UnityCatalog", "Represents a Unity Catalog in Databricks for data governance and management.")]
[BicepDocExample(
    "Creating a basic managed catalog",
    "This example shows how to create a basic managed Unity Catalog.",
    @"resource catalog 'UnityCatalog' = {
  name: 'my_catalog'
  comment: 'Main data catalog for analytics'
  owner: 'data-team@company.com'
  enablePredictiveOptimization: 'ENABLE'
}
"
)]
[BicepDocExample(
    "Creating a catalog with storage root",
    "This example shows how to create a catalog with a custom storage location.",
    @"resource catalogWithStorage 'UnityCatalog' = {
  name: 'analytics_catalog'
  comment: 'Analytics catalog with custom storage'
  storageRoot: 'abfss://container@storageaccount.dfs.core.windows.net/catalogs/analytics'
  owner: 'analytics-team@company.com'
  isolationMode: 'ISOLATED'
  enablePredictiveOptimization: 'INHERIT'
  properties: {
    department: 'analytics'
    cost_center: 'CC123'
  }
}
"
)]
[BicepDocExample(
    "Creating a catalog for external data sharing",
    "This example shows how to create a catalog connected to external data sharing.",
    @"resource sharingCatalog 'UnityCatalog' = {
  name: 'external_data_catalog'
  comment: 'Catalog for external data sharing'
  connectionName: 'external-connection'
  providerName: 'external-provider'
  shareName: 'shared-dataset'
  owner: 'data-governance@company.com'
  forceDestroy: false
}
"
)]
[BicepDocCustom("Notes", @"When working with the 'UnityCatalog' resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
param workspaceUrl string
extension databricksExtension with {
  workspaceUrl: workspaceUrl
}

// main.bicepparam
using 'main.bicep'
param workspaceUrl = '<workspaceUrl>'
```

Please note the following important considerations when using the `UnityCatalog` resource:

- Unity Catalog requires a metastore to be enabled in your workspace
- Catalog names must be unique within the metastore and follow naming conventions (alphanumeric and underscores)
- The `storageRoot` must be accessible by the Databricks workspace
- Use `forceDestroy: true` only when you're certain you want to delete the catalog and all its contents
- Predictive optimization settings can be inherited from parent objects or set explicitly
- External catalogs require proper connection and sharing configurations")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Unity Catalog API documentation][00]
- [Unity Catalog concepts][01]
- [Managing catalogs in Unity Catalog][02]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/catalogs/create
[01]: https://docs.databricks.com/data-governance/unity-catalog/index.html
[02]: https://docs.databricks.com/data-governance/unity-catalog/create-catalogs.html")]
[ResourceType("UnityCatalog")]
public class UnityCatalog : UnityCatalogIdentifiers
{
    // Configuration properties
    [TypeProperty("User-provided free-form text description.", ObjectTypePropertyFlags.None)]
    public string? Comment { get; set; }

    [TypeProperty("The name of the connection to an external data source.", ObjectTypePropertyFlags.None)]
    public string? ConnectionName { get; set; }

    [TypeProperty("A map of key-value properties attached to the securable.", ObjectTypePropertyFlags.None)]
    public object? Options { get; set; }

    [TypeProperty("A map of key-value properties attached to the securable.", ObjectTypePropertyFlags.None)]
    public object? Properties { get; set; }

    [TypeProperty("For catalogs corresponding to a share: the name of the provider.", ObjectTypePropertyFlags.None)]
    public string? ProviderName { get; set; }

    [TypeProperty("For catalogs corresponding to a share: the name of the share.", ObjectTypePropertyFlags.None)]
    public string? ShareName { get; set; }

    [TypeProperty("Storage root URL for managed tables within catalog.", ObjectTypePropertyFlags.None)]
    public string? StorageRoot { get; set; }

    [TypeProperty("Whether to force destroy the catalog even if it contains schemas and tables.", ObjectTypePropertyFlags.None)]
    public bool ForceDestroy { get; set; }

    // Read-only outputs
    [TypeProperty("Whether the catalog is accessible from all workspaces or a specific set of workspaces.", ObjectTypePropertyFlags.ReadOnly)]
    public bool BrowseOnly { get; set; }

    [TypeProperty("The type of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CatalogType? CatalogType { get; set; }

    [TypeProperty("Time at which this catalog was created, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int CreatedAt { get; set; }

    [TypeProperty("Username of catalog creator.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatedBy { get; set; }

    [TypeProperty("The effective predictive optimization flag.", ObjectTypePropertyFlags.ReadOnly)]
    public EffectivePredictiveOptimizationFlag? EffectivePredictiveOptimizationFlag { get; set; }

    [TypeProperty("Whether predictive optimization should be enabled for this object and objects under it.", ObjectTypePropertyFlags.None)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PredictiveOptimizationFlag? EnablePredictiveOptimization { get; set; }

    [TypeProperty("The full name of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    public string? FullName { get; set; }

    [TypeProperty("Whether the catalog is accessible from all workspaces or a specific set of workspaces.", ObjectTypePropertyFlags.None)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IsolationMode? IsolationMode { get; set; }

    [TypeProperty("Unique identifier of the metastore for the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    public string? MetastoreId { get; set; }

    [TypeProperty("Username of current owner of catalog.", ObjectTypePropertyFlags.None)]
    public string? Owner { get; set; }

    [TypeProperty("Provisioning info about the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    public ProvisioningInfo? ProvisioningInfo { get; set; }

    [TypeProperty("The type of the securable.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SecurableType? SecurableType { get; set; }

    [TypeProperty("Path to the storage location.", ObjectTypePropertyFlags.ReadOnly)]
    public string? StorageLocation { get; set; }

    [TypeProperty("Time at which this catalog was last modified, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int UpdatedAt { get; set; }

    [TypeProperty("Username of user who last modified catalog.", ObjectTypePropertyFlags.ReadOnly)]
    public string? UpdatedBy { get; set; }
}

public class UnityCatalogIdentifiers
{
    [TypeProperty("Name of catalog.", ObjectTypePropertyFlags.Required)]
    public string Name { get; set; } = string.Empty;
}

public class EffectivePredictiveOptimizationFlag
{
    [TypeProperty("The name of the object from which the flag was inherited.", ObjectTypePropertyFlags.ReadOnly)]
    public string? InheritedFromName { get; set; }

    [TypeProperty("The type of the object from which the flag was inherited.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InheritedFromType? InheritedFromType { get; set; }

    [TypeProperty("The value of the effective predictive optimization flag.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PredictiveOptimizationFlag? Value { get; set; }
}

public class ProvisioningInfo
{
    [TypeProperty("The current provisioning state of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisioningState? State { get; set; }
}
