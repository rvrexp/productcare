namespace ProductCare.Api.Authorization
{
    public static class ProductCarePolicies
    {
        public const string User = "ProductCare.User";
        public const string ActiveUser = "ProductCare.ActiveUser";
        public const string Admin = "ProductCare.Admin";

        public const string AccessAsUserScope = "access_as_user";
        public const string AdministratorRole = "ProductCare.Admin";
    }
}