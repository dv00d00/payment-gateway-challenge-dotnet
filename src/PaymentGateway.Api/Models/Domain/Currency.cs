﻿using System.Collections.Immutable;

using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Types;

using static PaymentGateway.Api.Constants.ErrorCodes.PaymentGateway;

namespace PaymentGateway.Api.Models.Domain;

public record Currency
{
    private static readonly ImmutableHashSet<string> Allowed = ["USD", "EUR", "GBP"];
    private static readonly string InvalidCurrencyErrorMessage = $"Currency must be one of: {string.Join(", ", Allowed)}";
    
    public string Value { get; }
    
    private Currency(string value)
    {
        Value = value;
    }
    
    public static Result<Currency> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Currency>.Failure(new Error(CurrencyRequired, "Currency is required."));
        
        value = value.ToUpperInvariant();

        if (value.Length != 3)
            return Result<Currency>.Failure(new Error(CurrencyLength, "Currency must be exactly 3 characters."));

        if (!Allowed.Contains(value))
            return Result<Currency>.Failure(new Error(CurrencyInvalid, InvalidCurrencyErrorMessage));

        return Result<Currency>.Success(new Currency(value));
    }
}