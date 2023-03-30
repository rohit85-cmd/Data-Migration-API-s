using CSVREADER.Data;
using CSVREADER.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection"));
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Staff API",
        Description = "An ASP.NET Core Web API for managing Staff Information",
        
    });
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddControllersWithViews();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Staff API V1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
