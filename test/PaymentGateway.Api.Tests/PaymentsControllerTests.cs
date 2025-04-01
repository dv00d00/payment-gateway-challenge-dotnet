using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Types;

using Xunit.Abstractions;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests(ITestOutputHelper output)
{
    private readonly Random _random = new();
    
    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new StoredPayment.Authorized
        {
            Id = Guid.NewGuid(),
            Money = Money.TryCreate(_random.Next(1, 10000), Currency.TryCreate("GBP").Value).Value,
            CardNumberLastFour = CardNumberLastFour.FromCardNumber(CardNumber.TryCreate("1234567890123456").Value),
            AuthorizationCode = "...",
            ExpiryDate = ExpiryDate.TryCreate(_random.Next(1, 12), _random.Next(2026, 2030)).Value
        };
        
        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Payments.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task Returns400_WhenRequestIsInvalid()
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IPaymentValidator>();
                    services.AddSingleton<IPaymentValidator>(_ => new StubValidator(Result<PaymentDetails>.Failure(
                        new Error("Invalid", "Test validation failed"))));
                }));

        var client = webApplicationFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/payments", ValidPostRequest());
        var raw = await response.Content.ReadAsStringAsync();
        output.WriteLine(raw);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorResponse = await response.Content.ReadFromJsonAsync<ClientErrorResponse>();
        Assert.NotNull(errorResponse);
        Assert.Contains(errorResponse.Issues, issue => issue.ErrorMessage == "Test validation failed");
    }
    
    [Theory]
    [InlineData(typeof(BankResponse.Declined), HttpStatusCode.OK)]
    [InlineData(typeof(BankResponse.Rejected), HttpStatusCode.BadRequest)]
    [InlineData(typeof(BankResponse.CommunicationError), HttpStatusCode.BadGateway, Justification.UnrecognizedResponse)]
    [InlineData(typeof(BankResponse.CommunicationError), HttpStatusCode.GatewayTimeout, Justification.Timeout)]
    [InlineData(typeof(BankResponse.CommunicationError), HttpStatusCode.InternalServerError, Justification.Exception)]
    public async Task HandlesAllBankResponses(Type responseType, HttpStatusCode expectedStatus, Justification? reason = null)
    {
        BankResponse bankResponse = responseType switch
        {
            var t when t == typeof(BankResponse.Declined) => new BankResponse.Declined(),
            var t when t == typeof(BankResponse.Rejected) => new BankResponse.Rejected("Declined by bank"),
            var t when t == typeof(BankResponse.CommunicationError) => new BankResponse.CommunicationError(reason!.Value),
            _ => throw new InvalidOperationException()
        };

        var factory = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IPaymentValidator>(_ => new StubValidator(ValidPaymentDetails()));
                    services.AddSingleton<IBankClient>(_ => new StubBankClient(bankResponse));
                    services.AddSingleton(new PaymentsRepository());
                }));

        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/payments", ValidPostRequest());

        Assert.Equal(expectedStatus, response.StatusCode);
    }
    
    [Fact]
    public async Task Returns200_WhenBankAuthorizesPayment()
    {
        var authorizationCode = AuthorizationCode.TryCreate("AUTH123").Value;

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IPaymentValidator>(_ => new StubValidator(ValidPaymentDetails()));
                    services.AddSingleton<IBankClient>(_ => new StubBankClient(new BankResponse.Authorized(authorizationCode)));
                    services.AddSingleton(new PaymentsRepository());
                }));

        var client = webApplicationFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/payments", ValidPostRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        Assert.NotNull(paymentResponse);
    }
    
    private class StubValidator(Result<PaymentDetails> result) : IPaymentValidator
    {
        public Result<PaymentDetails> Validate(PostPaymentRequest request) => result;
    }

    private class StubBankClient(BankResponse response) : IBankClient
    {
        public Task<BankResponse> InitiatePayment(PaymentDetails paymentDetails) => Task.FromResult(response);
    }

    private static PostPaymentRequest ValidPostRequest() => new()
    {
        CardNumber = "4111111111111111",
        ExpiryMonth = 12,
        ExpiryYear = 2030,
        Currency = "GBP",
        Amount = 500,
        CVV = "123"
    };

    private static Result<PaymentDetails> ValidPaymentDetails()
    {
        var cardNumber = CardNumber.TryCreate("4111111111111111").Value;
        var expiry = ExpiryDate.TryCreate(12, 2030).Value;
        var future = FutureExpiryDate.TryCreate(expiry, new FixedSystemTime(new DateOnly(2025, 3, 30))).Value;
        var currency = Currency.TryCreate("GBP").Value;
        var cvv = CVV.TryCreate("123").Value;
        var money = Money.TryCreate(500, currency).Value;

        return Result<PaymentDetails>.Success(new PaymentDetails(cardNumber, future, money, cvv));
    }

    
}