using PaymentGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<PaymentsRepository>();

builder.Services.AddHttpClient("payments", client =>
{
    client.BaseAddress = new Uri("http://localhost:8080/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<ISystemTime, SystemSystemTime>();
builder.Services.AddSingleton<IBankClient, BankClient>();
builder.Services.AddSingleton<IPaymentValidator, PaymentValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// todo: comb swagger
// todo: retry policy and circuit breaker and **idempotency**
// todo: http client settings
// todo: logging, metrics, and tracing
// todo: health check endpoint
// todo: authentication, authorization