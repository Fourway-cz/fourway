using Fourway.Api.Infrastructure.Database;
using Fourway.Api.Infrastructure.Endpoints;
using Fourway.Api.Infrastructure.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddEndpointServices();
builder.Services.AddOpenApiDocumentation();

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseEndpointPipeline();
app.UseOpenApiDocumentation();

app.Run();
