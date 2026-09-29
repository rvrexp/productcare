using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductIdentification
{
    public sealed record ProductImage(byte[] Content, string MediaType);
}
