var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapPost("/Predict", Predict);
app.MapRazorPages();

app.Run();

static IResult Predict(Dictionary<string, double>? model)
{
    if (model is null)
    {
        return Results.BadRequest(new { Message = "Request body must be a JSON object like { \"str\": 123 }." });
    }

    return Results.Ok(new
    {
        Message = "Prediction request received.",
        Model = model,
    });
}
