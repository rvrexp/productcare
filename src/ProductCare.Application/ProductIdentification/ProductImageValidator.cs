using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductIdentification
{
    public static class ProductImageValidator
    {
        //5 MB limit for image size
        public const int MaxSizeBytes = 5 * 1024 * 1024;

        public static string? Validate(ReadOnlySpan<byte> content, string mediaType)
        {
            if (content.IsEmpty)
            {
                return "The image is empty.";
            }

            if (content.Length > MaxSizeBytes)
            {
                return "The image exceeds the 5 MB limit.";
            }

            if (mediaType is not "image/jpeg"
                and not "image/png"
                and not "image/webp")
            {
                return "Only JPEG, PNG, and WebP images are supported.";
            }

            if (!HasExpectedSignature(content, mediaType))
            {
                return "The file content does not match the declared image type.";
            }

            return null;
        }

        private static bool HasExpectedSignature(ReadOnlySpan<byte> content, string mediaType)
        {
            return mediaType switch
            {
                "image/jpeg" =>
                    content.Length >= 3
                    && content[0] == 0xFF
                    && content[1] == 0xD8
                    && content[2] == 0xFF,

                "image/png" =>
                    content.Length >= 8
                    && content[0] == 0x89
                    && content[1] == 0x50
                    && content[2] == 0x4E
                    && content[3] == 0x47
                    && content[4] == 0x0D
                    && content[5] == 0x0A
                    && content[6] == 0x1A
                    && content[7] == 0x0A,

                "image/webp" =>
                    content.Length >= 12
                    && content[0] == 0x52
                    && content[1] == 0x49
                    && content[2] == 0x46
                    && content[3] == 0x46
                    && content[8] == 0x57
                    && content[9] == 0x45
                    && content[10] == 0x42
                    && content[11] == 0x50,

                _ => false
            };
        }
    }
}
