// using System.Collections.Immutable;
//
// using FluentValidation;
//
// using PaymentGateway.Api.Constants;
// using PaymentGateway.Api.Models.Requests;
//
// namespace PaymentGateway.Api.Services;
//
// public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
// {
//     private static readonly ImmutableHashSet<string> AllowedCurrencies = ["USD", "EUR", "GBP"];
//
//     public PostPaymentRequestValidator(ISystemTime systemTime)
//     {
//         RuleFor(x => x.CardNumber)
//             .NotEmpty()
//                 .WithMessage("Card number is required.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.CardNumberRequired)
//             .Length(14, 19)
//                 .WithMessage("Card number must be between 14 and 19 characters.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.CardNumberLength)
//             .Matches("^[0-9]+$")
//                 .WithMessage("Card number must only contain numeric characters.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.CardNumberNumeric);
//
//         RuleFor(x => x.ExpiryMonth)
//             .NotEmpty()
//                 .WithMessage("Expiry month is required.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.ExpiryMonthRequired)
//             .InclusiveBetween(1, 12)
//                 .WithMessage("Expiry month must be between 1 and 12.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.ExpiryMonthRange);
//
//         RuleFor(x => x.ExpiryYear)
//             .NotEmpty()
//                 .WithMessage("Expiry year is required.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.ExpiryYearRequired)
//             .Must(year => year >= systemTime.UtcToday.Year)
//                 .WithMessage("Expiry year must be this year or in the future.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.ExpiryYearPast)
//             .Must(year => year <= 9999)
//                 .WithMessage("Expiry year must be less than 10000.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.ExpiryYearTooLarge);
//
//         RuleFor(x => new { x.ExpiryMonth, x.ExpiryYear })
//             .Must(request => IsExpiryDateValid(systemTime, request.ExpiryMonth, request.ExpiryYear))
//             .WithName("Expiry Date")
//             .WithMessage("The expiry date must be in the future.")
//             .WithErrorCode(ErrorCodes.PaymentGateway.ExpiryDateInPast);
//
//         RuleFor(x => x.Currency)
//             .NotEmpty()
//                 .WithMessage("Currency is required.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.CurrencyRequired)
//             .Length(3)
//                 .WithMessage("Currency must be exactly 3 characters.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.CurrencyLength)
//             .Must(code => AllowedCurrencies.Contains(code))
//                 .WithMessage($"Currency must be one of the allowed codes: {string.Join(", ", AllowedCurrencies)}")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.CurrencyInvalid);
//
//         RuleFor(x => x.Amount)
//             .NotEmpty()
//                 .WithMessage("Amount is required.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.AmountRequired)
//             .GreaterThan(0)
//                 .WithMessage("Amount must be greater than zero.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.AmountNonPositive);
//
//         RuleFor(x => x.CVV)
//             .NotEmpty()
//                 .WithMessage("CVV is required.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.CvvRequired)
//             .Length(3, 4)
//                 .WithMessage("CVV must be between 3 and 4 characters.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.CvvLength)
//             .Matches("^[0-9]+$")
//                 .WithMessage("CVV must only contain numeric characters.")
//                 .WithErrorCode(ErrorCodes.PaymentGateway.CvvNumeric);
//     }
//
//     private bool IsExpiryDateValid(ISystemTime systemTime, int month, int year)
//     {
//         try
//         {
//             var expiryDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
//             return DateOnly.FromDateTime(expiryDate) >= systemTime.UtcToday;
//         }
//         catch
//         {
//             return false;
//         }
//     }
// }
