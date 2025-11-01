using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins("*")
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});
// Configure Kestrel for both local development and Cloud Run
string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
if (!int.TryParse(port, out int parsedPort))
{
    Console.WriteLine($"Warning: Invalid PORT environment variable value: {port}. Using default port 8080.");
    parsedPort = 8080;
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(parsedPort, configure => 
    {
        // Enable both HTTP1 and HTTP2
        configure.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
    Console.WriteLine($"Server configured to listen on port {parsedPort}");
});

// Disable request body size limit for Cloud Run
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = null;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger for all environments
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyWebAppProduct API v1");
    // Set the Swagger UI at the root
    options.RoutePrefix = "swagger";
});

// Disable HTTPS redirection on Cloud Run
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.UseCors("AllowAngularApp");
app.MapControllers();

app.Run();
