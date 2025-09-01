using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using DevOpsExtension.Handlers;
using DevOpsExtension.Models;

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
    .WithResourceHandler<AzureDevOpsRepositoryHandler>()
    .WithResourceHandler<AzureDevOpsArtifactFeedHandler>()
    .WithResourceHandler<AzureDevOpsServiceConnectionHandler>();

var app = builder.Build();

app.MapBicepExtension();

await app.RunAsync();
