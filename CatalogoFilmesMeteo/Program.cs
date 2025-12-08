using CatalogoFilmesMeteo.Repositories;
using CatalogoFilmesMeteo.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Registrar serviços de API externas
builder.Services.AddScoped<IServicoTmdbApi, ServicoTmdbApi>();
builder.Services.AddScoped<IServicoApiTempo, ServicoApiTempo>();

// Registrar repositório e serviços de negócio
builder.Services.AddScoped<IFilmeRepository, FilmeRepository>();
builder.Services.AddScoped<IFilmeImportService, FilmeImportService>();
builder.Services.AddScoped<IExportService, ExportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();