var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
// builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalHostFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://supplierdata1.netlify.app",
            "https://supplier-data-frontend-col9n4cik-govind789s-projects.vercel.app",
            "https://supplierdatafrontend.onrender.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
}); 

builder.Services.AddControllers();
var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// }

app.UseRouting();

// ✅ Must come before UseAuthorization or any other middleware
app.UseCors("AllowLocalHostFrontend");
 
app.UseAuthorization();
app.MapControllers();
app.Run();
