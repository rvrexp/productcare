namespace ProductCare.Domain.Access
{
    public static class AccessRequestPurposes
    {
        public const string PortfolioReview = "Portfolio / Interview Review";
        public const string TechnicalEvaluation = "Technical Evaluation";
        public const string Other = "Other";

        public static bool IsSupported(string? purpose)
        {
            return purpose is PortfolioReview or TechnicalEvaluation or Other;
        }
    }
}