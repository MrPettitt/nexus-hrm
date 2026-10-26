using NexusHRM.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// In-memory demo data. Restarting the app resets everything to the seed state.
builder.Services.AddSingleton<DummyData>();

// Session holds the signed-in employee id for the demo.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();

app.Run();
