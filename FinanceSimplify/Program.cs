using System.Text;
using FinanceSimplify.Data;
using FinanceSimplify.Services.AuthService;
using FinanceSimplify.Services.BankAccountService;
using FinanceSimplify.Services.CardService;
using FinanceSimplify.Services.Category;
using FinanceSimplify.Services.PasswordService;
using FinanceSimplify.Services.TransactionService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IAuthInterface, AuthService>();
builder.Services.AddScoped<IPasswordInterface, PasswordService>();
builder.Services.AddScoped<ITransactionInterface, TransactionService>();
builder.Services.AddScoped<ICardInterface, CardService>();
builder.Services.AddScoped<ICategoryInterface, CategoryService>();
builder.Services.AddScoped<IBankAccountInterface, BankAccountService>();

// Configure MongoDB
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();

// Configure MongoDB client settings for Railway/Linux compatibility
var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoSettings!.ConnectionString);
mongoClientSettings.SslSettings = new MongoDB.Driver.SslSettings
{
    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
    CheckCertificateRevocation = false
};
mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(30);

builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoClientSettings));
builder.Services.AddScoped<IMongoDatabase>(sp => {
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoSettings!.DatabaseName);
});
builder.Services.AddScoped<MongoDbContext>();



builder.Services.AddSwaggerGen(options => {

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme {
        Description = "Standar Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();

});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!)),
        ValidateAudience = false,
        ValidateIssuer = false
    };
});





var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

// CORS must come before Authentication and Authorization
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
