using Mall.Api.Common;
using Mall.Api.Endpoints;
using Mall.Api.Infrastructure;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddProblemDetails()
    .AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddApiOptions(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCors(options => options.AddPolicy(ApiConstants.CorsPolicyName, policy =>
    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true)));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors(ApiConstants.CorsPolicyName);
app.UseAuthorization();

app.MapHealthChecks("/health").WithTags("System");
app.MapApiEndpoints();
app.Run();

public partial class Program;
