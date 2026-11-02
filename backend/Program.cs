using Fourway.Api.Infrastructure.Endpoints;
using Fourway.Api.Infrastructure.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointServices();
builder.Services.AddOpenApiDocumentation();

var app = builder.Build();

app.UseEndpointPipeline();
app.UseOpenApiDocumentation();

app.Run();
