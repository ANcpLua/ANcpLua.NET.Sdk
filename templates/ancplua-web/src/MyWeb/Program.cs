var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", static () => "MyWeb");
app.MapHealthChecks("/health");

app.Run();

namespace MyWeb
{
    public partial class Program;
}
