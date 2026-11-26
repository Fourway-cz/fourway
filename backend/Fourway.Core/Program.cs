using Fourway.Core.Infrastructure.Database;
using Fourway.Core.Infrastructure.Endpoints;
using Fourway.Core.Infrastructure.OpenApi;
using Fourway.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJourneyServices();
builder.Services.AddEndpointServices();
builder.Services.AddOpenApiDocumentation();

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseEndpointPipeline();
app.UseOpenApiDocumentation();

app.Run();
