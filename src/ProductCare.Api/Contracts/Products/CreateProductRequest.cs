using System.ComponentModel.DataAnnotations;

namespace ProductCare.Api.Contracts.Products
{
    public sealed record CreateProductRequest(

        [MaxLength(200)] string? Manufacturer,

        [MaxLength(100)] string? ModelNumber,

        [MaxLength(100)] string? ProductType,

        [MaxLength(200)] string? ProductName,

        [MaxLength(150)] string? SerialNumber,

        [MaxLength(16)] string? IdentificationConfidence);
}
