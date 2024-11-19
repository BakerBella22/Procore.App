using PdfSharp.Charting;
using Procore.App.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Register code pages encoding provider (necessary for HtmlRenderer.PdfSharp)
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Load configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                     .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                     .AddUserSecrets<Program>();

builder.Services.AddHostedService<QueueListenerService>();

// Register services
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<QueueService>();

// Register Client with DI, providing configuration parameters
builder.Services.AddScoped(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var clientId = configuration["ProcoreClientId"];
    var clientSecret = configuration["ProcoreClientSecret"];
    var isSandbox = bool.Parse(configuration["ProcoreIsSandbox"] ?? "false");
    var baseUrl = configuration["ProcoreBaseUrl"];
    var companyId = configuration["ProcoreCompanyId"];

    var config = new Procore.Core.Config(clientId, clientSecret, isSandbox, baseUrl);
    return new Procore.Core.Client(config, companyId);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();