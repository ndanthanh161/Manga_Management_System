using Microsoft.EntityFrameworkCore;
using MMS.Infrastructure.Data;
using MMS.NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký gRPC server
builder.Services.AddGrpc();

// Database (cùng connection string, read-only để lookup user email)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MmsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Đăng ký email service
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IUserEmailLookup, DbUserEmailLookup>();

// Cấu hình từ appsettings
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

var app = builder.Build();

// Map gRPC service
app.MapGrpcService<NotificationGrpcService>();

// Health check endpoint
app.MapGet("/", () => "MMS NotificationService gRPC is running");

app.Run();
