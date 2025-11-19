using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using DevOpsExtension;
using DevOpsExtension.ArtifactFeed;
using DevOpsExtension.Extension;
using Microsoft.Extensions.DependencyInjection;
using DevOpsExtension.Permission;
using DevOpsExtension.Project;
using DevOpsExtension.Repository;
using DevOpsExtension.ServiceConnection;
using DevOpsExtension.WorkItems;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);
builder.Services
    .AddBicepExtension(
        name: "AzureDevOps",
        version: ThisAssembly.AssemblyInformationalVersion.Split('+')[0],
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly,
        configurationType: typeof(Configuration))
    .WithResourceHandler<AzureDevOpsProjectHandler>()
    .WithResourceHandler<AzureDevOpsWorkItemHandler>()
    .WithResourceHandler<AzureDevOpsRepositoryHandler>()
    .WithResourceHandler<AzureDevOpsArtifactFeedHandler>()
    .WithResourceHandler<AzureDevOpsServiceConnectionHandler>()
    .WithResourceHandler<AzureDevOpsPermissionHandler>()
    .WithResourceHandler<AzureDevOpsExtensionHandler>();

var app = builder.Build();

app.MapBicepExtension();

await app.RunAsync();
