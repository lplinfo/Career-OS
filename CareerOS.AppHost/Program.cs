var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL container resource
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "16-alpine")
    .WithDataVolume();

var careerDatabase = postgres.AddDatabase("CareerDatabase", "careeros");

// OpenBao container resource
var openbao = builder.AddContainer("openbao", "openbao/openbao")
    .WithHttpEndpoint(port: 8200, targetPort: 8200, name: "http")
    .WithEnvironment("BAO_DEV_ROOT_TOKEN_ID", "root")
    .WithEnvironment("BAO_DEV_LISTEN_ADDRESS", "0.0.0.0:8200")
    .WithEnvironment("VAULT_DEV_ROOT_TOKEN_ID", "root")
    .WithEnvironment("VAULT_DEV_LISTEN_ADDRESS", "0.0.0.0:8200")
    .WithVolume("openbao_data", "/openbao/data");

var jwtSecretKey = builder.Configuration["JwtOptions:SecretKey"]
    ?? builder.Configuration["JwtOptions__SecretKey"]
    ?? "9bf0a6bfb77ea9f87f72791ef41137ed";

// Backend ASP.NET Core API
var api = builder.AddProject<Projects.CareerOS_Api>("careeros-api", launchProfileName: "https")
    .WithReference(careerDatabase)
    .WithEnvironment("OpenBao__Enabled", "true")
    .WithEnvironment("BAO_ADDR", openbao.GetEndpoint("http"))
    .WithEnvironment("OpenBao__Address", openbao.GetEndpoint("http"))
    .WithEnvironment("JwtOptions__SecretKey", jwtSecretKey);

// Frontend Angular application
builder.AddNpmApp("careeros-frontend", "../frontend", scriptName: "start")
    .WithHttpEndpoint(port: 4200, targetPort: 4200, name: "http")
    .WithReference(api);

builder.Build().Run();
