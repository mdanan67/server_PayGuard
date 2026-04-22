using AutoMapper;
using Microsoft.EntityFrameworkCore;
using server.model;
using server.Profiles;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(UserProfile));
builder.Services.AddDbContext<AppDBContext>(option => option.UseNpgsql(builder.Configuration
.GetConnectionString("ConnectionStrings")));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapGet("/", () =>
{
    return "hello";
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");


app.MapControllers();


app.Run();

