using System.Net;
using System.Net.Http.Json;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Domain;
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/Payments");
            requestMessage.Content = JsonContent.Create(request);
            var response = await client.SendAsync(requestMessage);

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        }).QuickCheckThrowOnFailure(output);
    }

    [Fact]
    public void HashRules()
    {
        var arb = PaymentRequestGenerators.ValidPaymentRequestGen(PaymentStatus.Authorized, DateTime.UtcNow);
        var validator = new PaymentValidator(new FixedSystemTime(DateOnly.FromDateTime(DateTime.Now)));
        
        Prop.ForAll(arb.Two().ToArbitrary(), gen =>
        {
            var (r1, r2) = gen;
            PaymentDetails paymentDetails1 = validator.Validate(r1).Value;
            PaymentDetails paymentDetails2 = validator.Validate(r2).Value;

            var hash1 = RequestHashing.ComputeHash(paymentDetails1);
            var hash2 = RequestHashing.ComputeHash(paymentDetails2);
            
            Assert.Equal(paymentDetails1 == paymentDetails2, hash1 == hash2);

        }).QuickCheckThrowOnFailure(output);
        
        Prop.ForAll(arb.ToArbitrary(), r1 =>
        {
            var hash1 = RequestHashing.ComputeHash(validator.Validate(r1).Value);
            var hash2 = RequestHashing.ComputeHash(validator.Validate(r1).Value);
            
            Assert.Equal(hash1, hash2);

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
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var arb = PaymentRequestGenerators.ValidPaymentRequestGen(expectedOutcome, DateTime.UtcNow).ToArbitrary();
        
        Prop.ForAll(arb, async request =>
        {
            paymentsRepository.Payments.Clear();
            
            var initiatePaymentResponseMessage = await client.PostAsJsonAsync("/api/Payments", request);
            var responseOnInitiate = await initiatePaymentResponseMessage.Content.ReadFromJsonAsync<PostPaymentResponse>();

            Assert.Equal(HttpStatusCode.OK, initiatePaymentResponseMessage.StatusCode);
            Assert.NotNull(responseOnInitiate);
            Assert.Equal(expectedOutcome, responseOnInitiate.Status);

            var saved = paymentsRepository.Payments.Single();
            
            AssertResponse(responseOnInitiate, request, saved);

            var findPaymentResponseMessage = await client.GetAsync($"/api/Payments/{saved.Id}");
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
}