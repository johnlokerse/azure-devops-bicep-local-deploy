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
        version: "0.0.1",
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly,
        configurationType: typeof(Configuration))
    .WithResourceHandler<AzureDevOpsProjectHandler>()
    .WithResourceHandler<AzureDevOpsRepositoryHandler>();

var app = builder.Build();

app.MapBicepExtension();

await app.RunAsync();
