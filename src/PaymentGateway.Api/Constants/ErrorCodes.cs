namespace PaymentGateway.Api.Constants;

public static class ErrorCodes
{
    public static class PaymentGateway
    {
        public const string CardNumberRequired = "ERR_CARD_NUMBER_REQUIRED";
        public const string CardNumberLength = "ERR_CARD_NUMBER_LENGTH";
        public const string CardNumberNumeric = "ERR_CARD_NUMBER_NUMERIC";

        public const string ExpiryMonthRequired = "ERR_EXPIRY_MONTH_REQUIRED";
        public const string ExpiryMonthRange = "ERR_EXPIRY_MONTH_RANGE";

        public const string ExpiryYearRequired = "ERR_EXPIRY_YEAR_REQUIRED";
        public const string ExpiryYearTooSmall = "ERR_EXPIRY_YEAR_TOO_SMALL";
        public const string ExpiryYearTooLarge = "ERR_EXPIRY_YEAR_TOO_LARGE";

        public const string ExpiryYearPast = "ERR_EXPIRY_YEAR_PAST";
        public const string ExpiryDateInPast = "ERR_EXPIRY_DATE_IN_PAST";

        public const string CurrencyRequired = "ERR_CURRENCY_REQUIRED";
        public const string CurrencyLength = "ERR_CURRENCY_LENGTH";
        public const string CurrencyInvalid = "ERR_CURRENCY_INVALID";

        public const string AmountRequired = "ERR_AMOUNT_REQUIRED";
        public const string AmountNonPositive = "ERR_AMOUNT_NON_POSITIVE";

        public const string CvvRequired = "ERR_CVV_REQUIRED";
        public const string CvvLength = "ERR_CVV_LENGTH";
        public const string CvvNumeric = "ERR_CVV_NUMERIC";
    }
    
    public static class AcquiringBank
    {
        public const string RejectedByBank = "ERR_REJECTED_BY_BANK";
        public const string AuthorizationCodeInvalid = "ERR_AUTHORIZATION_CODE_FROM_BANK_INVALID";
    }
}