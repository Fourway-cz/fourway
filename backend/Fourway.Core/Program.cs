using Fourway.Core.Infrastructure.Database;
using Fourway.Core.Infrastructure.Endpoints;
using Fourway.Core.Infrastructure.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddEndpointServices();
builder.Services.AddOpenApiDocumentation();

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseEndpointPipeline();
app.UseOpenApiDocumentation();

app.Run();
