using System.Text.Json;

namespace DevOpsExtension.Tests.Helpers;

/// <summary>
/// Static helper class for creating common Azure DevOps API response objects.
/// </summary>
public static class AzureDevOpsApiResponses
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static object CreateProjectResponse(
        string id = "00000000-0000-0000-0000-000000000001",
        string name = "test-project",
        string? description = null,
        string state = "wellFormed",
        string? url = null)
    {
        return new
        {
            id,
            name,
            description,
            state,
            url = url ?? $"https://dev.azure.com/testorg/_apis/projects/{id}"
        };
    }

    public static object CreateRepositoryResponse(
        string id = "00000000-0000-0000-0000-000000000002",
        string name = "test-repo",
        string? webUrl = null,
        string? remoteUrl = null,
        string? sshUrl = null,
        string? defaultBranch = null)
    {
        return new
        {
            id,
            name,
            webUrl = webUrl ?? $"https://dev.azure.com/testorg/test-project/_git/{name}",
            remoteUrl = remoteUrl ?? $"https://testorg@dev.azure.com/testorg/test-project/_git/{name}",
            sshUrl = sshUrl ?? $"git@ssh.dev.azure.com:v3/testorg/test-project/{name}",
            defaultBranch
        };
    }

    public static object CreateFeedResponse(
        string id = "00000000-0000-0000-0000-000000000003",
        string name = "test-feed",
        string? description = null,
        bool hideDeletedPackageVersions = true,
        bool upstreamEnabled = true,
        string? projectId = null,
        string? projectName = null)
    {
        var response = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["name"] = name,
            ["description"] = description,
            ["hideDeletedPackageVersions"] = hideDeletedPackageVersions,
            ["upstreamEnabled"] = upstreamEnabled,
            ["url"] = $"https://feeds.dev.azure.com/testorg/_apis/packaging/feeds/{id}"
        };

        if (!string.IsNullOrEmpty(projectId))
        {
            response["project"] = new { id = projectId, name = projectName };
        }

        return response;
    }

    public static object CreateServiceConnectionResponse(
        string id = "00000000-0000-0000-0000-000000000004",
        string name = "test-connection",
        string type = "AzureRM",
        string scheme = "WorkloadIdentityFederation",
        string? issuer = null,
        string? subject = null)
    {
        return new
        {
            value = new[]
            {
                new
                {
                    id,
                    name,
                    type,
                    url = "https://management.azure.com/",
                    authorization = new
                    {
                        scheme,
                        parameters = new
                        {
                            workloadIdentityFederationIssuer = issuer ?? "https://vstoken.dev.azure.com/testorg",
                            workloadIdentityFederationSubject = subject ?? "sc://testorg/test-project/test-connection"
                        }
                    }
                }
            }
        };
    }

    public static object CreateEmptyServiceConnectionResponse()
    {
        return new { value = Array.Empty<object>() };
    }

    public static object CreateExtensionResponse(
        string extensionId = "vss-code-search",
        string publisherId = "ms",
        string version = "1.0.0")
    {
        return new
        {
            extensionId,
            publisherId,
            extensionName = extensionId,
            publisherName = publisherId,
            version
        };
    }

    public static object CreateOperationResponse(
        string id = "00000000-0000-0000-0000-000000000099",
        string status = "succeeded")
    {
        return new { id, status };
    }

    public static object CreateProcessTemplatesResponse()
    {
        return new
        {
            value = new[]
            {
                new { id = "adcc42ab-9882-485e-a3ed-7678f01f66bc", name = "Agile" },
                new { id = "6b724908-ef14-45cf-84f8-768b5384da45", name = "Scrum" },
                new { id = "27450541-8e31-4150-9947-dc59f998fc01", name = "CMMI" },
                new { id = "b8a3a935-7e91-48b8-a94c-606d37c3e9f2", name = "Basic" }
            }
        };
    }

    public static object CreateGraphGroupsResponse(params (string descriptor, string originId, string displayName)[] groups)
    {
        return new
        {
            value = groups.Select(g => new
            {
                descriptor = g.descriptor,
                originId = g.originId,
                displayName = g.displayName,
                origin = "aad",
                subjectKind = "group"
            }).ToArray()
        };
    }

    public static object CreateProjectGraphGroupsResponse(string projectName, params string[] roleNames)
    {
        return new
        {
            value = roleNames.Select((name, index) => new
            {
                descriptor = $"vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAw{index}",
                displayName = name,
                principalName = $"[{projectName}]\\{name}",
                domain = "vstfs:///Classification/TeamProject/00000000-0000-0000-0000-000000000001"
            }).ToArray()
        };
    }

    public static object CreateGraphDescriptorResponse(string descriptor = "scp.MTIzNDU2Nzg5")
    {
        return new { value = descriptor };
    }

    public static object CreateGraphGroupImportResponse(
        string descriptor = "aadgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAw",
        string originId = "00000000-0000-0000-0000-000000000010")
    {
        return new
        {
            descriptor,
            originId,
            origin = "aad",
            subjectKind = "group"
        };
    }

    public static object CreateMembershipResponse()
    {
        return new
        {
            memberDescriptor = "aadgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAw",
            containerDescriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAw"
        };
    }

    public static object CreatePipelineListResponse(params (int id, string name)[] pipelines)
    {
        return new
        {
            value = pipelines.Select(p => new { id = p.id, name = p.name }).ToArray()
        };
    }

    public static object CreatePipelineRunResponse(
        int id = 1,
        string state = "inProgress",
        string? result = null)
    {
        return new
        {
            id,
            state,
            result,
            url = $"https://dev.azure.com/testorg/test-project/_apis/pipelines/1/runs/{id}"
        };
    }

    public static object CreateWorkItemSearchResponse(params int[] ids)
    {
        return new
        {
            workItems = ids.Select(id => new { id }).ToArray()
        };
    }
}
