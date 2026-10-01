using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductKnowledge
{
    public sealed record ProductKnowledgeAnswer(string Answer, IReadOnlyList<ProductKnowledgeCitation> Citations);
}
