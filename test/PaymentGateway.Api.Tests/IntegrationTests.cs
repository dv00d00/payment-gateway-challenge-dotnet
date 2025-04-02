using System.Net;
using System.Net.Http.Json;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using Xunit.Abstractions;

namespace PaymentGateway.Api.Tests;

public class IntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public void InvalidRequest_ResultsIn_BadResponse()
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var validator = webApplicationFactory.Services.GetRequiredService<IPaymentValidator>();
        var client = webApplicationFactory.CreateClient();
        var arb = PaymentRequestGenerators.InvalidPaymentRequestGen(validator).ToArbitrary();
        
        Prop.ForAll(arb, async request =>
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/Payments");
            requestMessage.Content = JsonContent.Create(request);
            var response = await client.SendAsync(requestMessage);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            
        }).QuickCheckThrowOnFailure(output);
    }

    [Theory]
    [InlineData(PaymentStatus.Rejected)]
    public void ValidRequest_503_On_Bank(PaymentStatus expectedOutcome)
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var paymentsRepository = new PaymentsRepository();
        
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository))
                )
            .CreateClient();

        var arb = PaymentRequestGenerators.ValidPaymentRequestGen(expectedOutcome, DateTime.UtcNow).ToArbitrary();
        
        Prop.ForAll(arb, async request =>
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/payments");
            requestMessage.Content = JsonContent.Create(request);
            requestMessage.Headers.Add(Names.Headers.IdempotencyKey, Guid.NewGuid().ToString());
            var response = await client.SendAsync(requestMessage);

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        }).QuickCheckThrowOnFailure(output);
    }
    
    [Theory]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Declined)]
    public void ValidRequest_ValidResponse(PaymentStatus expectedOutcome)
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var paymentsRepository = new PaymentsRepository();
        
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton<IPaymentsRepository>(paymentsRepository)))
            .CreateClient();

        var arb = PaymentRequestGenerators.ValidPaymentRequestGen(expectedOutcome, DateTime.UtcNow).ToArbitrary();
        
        Prop.ForAll(arb, async request =>
        {
            string id = Guid.NewGuid().ToString();
            
            paymentsRepository.Payments.Clear();
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/payments");
            requestMessage.Content = JsonContent.Create(request);
            requestMessage.Headers.Add(Names.Headers.IdempotencyKey, id);
            
            var initiatePaymentResponseMessage = await client.SendAsync(requestMessage);
            output.WriteLine(await initiatePaymentResponseMessage.Content.ReadAsStringAsync());
            var responseOnInitiate = await initiatePaymentResponseMessage.Content.ReadFromJsonAsync<PostPaymentResponse>();

            Assert.Equal(HttpStatusCode.OK, initiatePaymentResponseMessage.StatusCode);
            Assert.NotNull(responseOnInitiate);
            Assert.Equal(expectedOutcome, responseOnInitiate.Status);

            var saved = paymentsRepository.Payments.Values.Single();
            
            AssertResponse(responseOnInitiate, request, saved);

            var findPaymentResponseMessage = await client.GetAsync($"/api/payments/{id}");
            Assert.Equal(HttpStatusCode.OK, findPaymentResponseMessage.StatusCode);
            var responseOnFind = await findPaymentResponseMessage.Content.ReadFromJsonAsync<PostPaymentResponse>();
            
            Assert.NotNull(responseOnFind);
            Assert.Equal(expectedOutcome, responseOnFind.Status);
            AssertResponse(responseOnInitiate, request, saved);

            Assert.Equivalent(responseOnInitiate, responseOnFind);
            
        }).QuickCheckThrowOnFailure(output);
     
        static void AssertResponse(
            PostPaymentResponse response, 
            PostPaymentRequest request, 
            StoredPayment storedPayment)
        {
            Assert.Equal(request.Amount, response.Amount);
            Assert.Equal(int.Parse(request.CardNumber[^4..]), response.CardNumberLastFour);
            Assert.Equal(request.Currency, response.Currency);
            Assert.Equal(request.ExpiryMonth, response.ExpiryMonth);
            Assert.Equal(request.ExpiryYear, response.ExpiryYear);
            Assert.Equal(storedPayment.Id, response.Id);
            Assert.Equal(storedPayment.Status, response.Status);
        }
    }
    
    [Theory]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Declined)]
    public async Task SameIdempotencyKey_DifferentRequests_ShouldFail(PaymentStatus expectedOutcome)
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        
        var client = webApplicationFactory.CreateClient();

        var arb = PaymentRequestGenerators.ValidPaymentRequestGen(expectedOutcome, DateTime.UtcNow).ToArbitrary();
        
        string id = Guid.NewGuid().ToString();
        // initial request to block idempotency key
        var initialRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments");
        initialRequest.Content = JsonContent.Create(arb.Generator.Sample(1).Single());
        initialRequest.Headers.Add(Names.Headers.IdempotencyKey, id);
        _ = await client.SendAsync(initialRequest);
        
        Prop.ForAll(arb, async request =>
        {
            var subsequentRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments");
            subsequentRequest.Content = JsonContent.Create(request);
            subsequentRequest.Headers.Add(Names.Headers.IdempotencyKey, id);
            var initiatePaymentResponseMessage = await client.SendAsync(subsequentRequest);
            var response = await initiatePaymentResponseMessage.Content.ReadFromJsonAsync<ClientErrorResponse>();

            Assert.Equal(HttpStatusCode.BadRequest, initiatePaymentResponseMessage.StatusCode);
            Assert.Contains(response!.Issues, it => it.ErrorCode == ErrorCodes.Idempotency.IdempotencyKeyAlreadyUsed);
            
        }).QuickCheckThrowOnFailure(output);
        
    }
    
    [Theory]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Declined)]
    public async Task SameIdempotencyKey_SameRequests_SameResult(PaymentStatus expectedOutcome)
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        
        var client = webApplicationFactory.CreateClient();

        var arb = PaymentRequestGenerators.ValidPaymentRequestGen(expectedOutcome, DateTime.UtcNow).ToArbitrary();
        
        string id = Guid.NewGuid().ToString();
        var postPaymentRequest = arb.Generator.Sample(1).Single();

        // initial request to block idempotency key
        var initialRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments");
        initialRequest.Content = JsonContent.Create(postPaymentRequest);
        initialRequest.Headers.Add(Names.Headers.IdempotencyKey, id);
        
        var initialResponse = await client.SendAsync(initialRequest);
        initialResponse.EnsureSuccessStatusCode();
        output.WriteLine(await initialResponse.Content.ReadAsStringAsync());
        var initialDto = await initialResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        for (int i = 0; i < 10; i++)
        {
            var subsequentRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments");
            subsequentRequest.Content = JsonContent.Create(postPaymentRequest);
            subsequentRequest.Headers.Add(Names.Headers.IdempotencyKey, id);
            var subsequentResponse = await client.SendAsync(subsequentRequest);
            
            output.WriteLine(await subsequentResponse.Content.ReadAsStringAsync());
            var subsequentDto = await subsequentResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();
            
            subsequentResponse.EnsureSuccessStatusCode();
            Assert.Equivalent(initialDto, subsequentDto);
        }
    }
}