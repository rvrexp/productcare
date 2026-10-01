using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductKnowledge
{
   public sealed record ProductKnowledgeQuery(string Manufacturer, string ModelNumber, string Question);
}
