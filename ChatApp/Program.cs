using ChatApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ChatDbContext>();

builder.Services.AddSignalR();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    db.Database.EnsureCreated();
}

app.UseStaticFiles();

app.UseRouting();

app.MapHub<ChatHub>("/chathub");
app.MapHub<ChatListHub>("/chatlisthub");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", async context =>
{
    context.Response.Redirect("/index.html");
    await Task.CompletedTask;
});

app.Run();
