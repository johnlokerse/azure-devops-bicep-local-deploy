using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using DevOpsExtension.Handlers;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);
builder.Services
    .AddBicepExtension(
        name: "AzureDevOpsExtension",
        version: "0.0.1",
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly)
    .WithResourceHandler<AzureDevOpsProjectHandler>()
    .WithResourceHandler<AzureDevOpsRepositoryHandler>();

var app = builder.Build();

app.MapBicepExtension();

await app.RunAsync();
