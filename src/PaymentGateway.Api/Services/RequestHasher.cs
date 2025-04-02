using System.Buffers;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.ObjectPool;

using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Services;

public static class RequestHasher
{
    private const int L = 19 + // Card number
                          1 + // -
                          2 + // ExpiryMonth
                          1 + // -
                          4 + // ExpiryYear
                          1 + // -
                          4 + // CVV
                          1 + // -
                          3 + // Currency
                          1 + // -
                          10; // Amount
    
    private const int ExpectedMaximumHashStubLength = L * 2;
    
    private static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider()
        .CreateStringBuilderPool(ExpectedMaximumHashStubLength, ExpectedMaximumHashStubLength);
    
    public static string ComputeHash(PaymentDetails request)
    {
        var rawBytes = ArrayPool<byte>.Shared.Rent(ExpectedMaximumHashStubLength * 2);
        var rawChars = ArrayPool<char>.Shared.Rent(ExpectedMaximumHashStubLength);
        var sb = StringBuilderPool.Get();
        
        try
        {
            sb.Append(request.CardNumber.Value)
                .Append('-')
                .Append(request.ExpiryDate.Month)
                .Append('-')
                .Append(request.ExpiryDate.Year)
                .Append('-')
                .Append(request.CVV.Value)
                .Append('-')
                .Append(request.Money.Currency.Value)
                .Append('-')
                .Append(request.Money.MinorCurrencyUnitCount);
            
            sb.CopyTo(0, rawChars, sb.Length);
            var bytesCount = Encoding.UTF8.GetBytes(rawChars.AsSpan(0, sb.Length), rawBytes);
            Span<byte> hash = stackalloc byte[256 / 8];
            SHA256.HashData(rawBytes.AsSpan(0, bytesCount), hash);
            return Convert.ToHexString(hash);
        }
        finally
        {
            StringBuilderPool.Return(sb);
            ArrayPool<byte>.Shared.Return(rawBytes);
            ArrayPool<char>.Shared.Return(rawChars);
        }
    }
}