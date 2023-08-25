// var builder = WebApplication.CreateBuilder(args);
// var app = builder.Build();

// app.MapGet("/", () => "Hello World!");

// app.Run();

using Twilio.AspNet.Core;
using SmsChatGpt;
using OpenAI.GPT3.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddTwilioClient();

builder.Services.AddOpenAIService();

var app = builder.Build();

app.UseSession();

app.MapMessageEndpoint();

app.Run();

