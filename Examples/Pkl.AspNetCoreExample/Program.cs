using Microsoft.Extensions.Configuration.Pkl;
using Microsoft.Extensions.Options;
using Pkl;
using Pkl.AspNetCoreExample.Config;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddPklModule(ModuleSource.FileSource("Config/Pkl/AppConfig.pkl"));
builder.Services.Configure<AppConfig>(builder.Configuration);

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<AppConfig>>();
var testConfiguration = options.Value;
Console.WriteLine(testConfiguration.Text);
Console.WriteLine(testConfiguration.Port);

app.MapGet("/", () => "Hello World!");

app.Run();
