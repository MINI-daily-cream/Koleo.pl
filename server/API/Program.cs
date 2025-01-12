using API.Services.Interfaces;
using Koleo.Models;
using Koleo.Services;
using KoleoPL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Persistence;
using SQLitePCL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using API.Interfaces;
using API.Services;
using Application.Users;
using Application;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


builder.Services.AddScoped<IDatabaseServiceAPI, DatabaseServiceAPI>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdsService, AdsService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IRankingService,RankingService>();
builder.Services.AddScoped<IStatisticsService,StatisticsService>();
builder.Services.AddScoped<ITicketServive, TicketService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGetInfoFromIdService, GetInfoFromIdService>();
builder.Services.AddScoped<IGetIdFromInfoService, GetIdFromInfoService>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddScoped<IAchievementsService, AchievementsService>();
builder.Services.AddScoped<IConnectionSeatsService, ConnectionSeatsService>();
builder.Services.AddTransient<TestKlasa>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")//,
        // b => b.MigrationsAssembly("Persistence")
    )
);
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
        //.SetIsOriginAllowedToAllowWildcardSubdomains()
        .AllowAnyMethod().AllowAnyHeader();
        //;
        //.AllowCredentials();
    });
});

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["TokenKey"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    ClockSkew = TimeSpan.Zero
                };
});

builder.Services.AddAuthorization(options => {
    options.AddPolicy("Admin", policy => 
        policy.RequireAssertion(context => context.User.HasClaim(c => (c.Type == "Role" && c.Value == "Admin"))));
});



builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(List.Handler).Assembly));

// builder.Services.AddHostedService<QueueConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization(); 

app.UseHttpsRedirection();

app.MapControllers();


using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<DataContext>();
    await context.Database.MigrateAsync();
    await Seed.ClearRankingsUsers(context);

    await Seed.SeedData(context);
    //Seed.ClearTickets(context);
    // Seed.ClearConnectionsEtc(context);
    await Seed.SeedConnectionsEtc(context);
    await Seed.SeedAchievements(context);
    await Seed.SeedRankings(context);
    await Seed.SeedTestUsersAndStaticsData(context);

}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occured during migration");
}

app.Run();