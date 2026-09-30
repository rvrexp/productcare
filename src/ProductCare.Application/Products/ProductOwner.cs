using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.Products
{
    public sealed record ProductOwner(string TenantId, string Subject);
}
