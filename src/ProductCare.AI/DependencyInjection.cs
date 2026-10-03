using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using ProductCare.AI.Configuration;
using ProductCare.AI.ProductAssistant;
using ProductCare.AI.ProductIdentification;
using ProductCare.AI.ProductKnowledge;
using ProductCare.Application.ProductAssistant;
using ProductCare.Application.ProductIdentification;
using ProductCare.Application.ProductKnowledge;

namespace ProductCare.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddProductCareAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FoundryOptions>().Bind(configuration.GetSection(FoundryOptions.SectionName))
                            .Validate(
                                options =>
                                    Guid.TryParse(options.TenantId, out _),
                                "Foundry TenantId must be a valid tenant ID.")

                            .Validate(
                                options =>
                                    Uri.TryCreate(
                                        options.ProjectEndpoint,
                                        UriKind.Absolute,
                                        out Uri? endpoint)
                                    && endpoint.Scheme == Uri.UriSchemeHttps,
                                "Foundry ProjectEndpoint must be a valid HTTPS URL.")

                            .Validate(
                                options =>
                                    !string.IsNullOrWhiteSpace(
                                        options.ModelDeployment),
                                "Foundry ModelDeployment is required.")
                            .ValidateOnStart();



        services.AddSingleton<ProjectResponsesClient>(
            serviceProvider =>
            {
                FoundryOptions settings = serviceProvider.GetRequiredService<IOptions<FoundryOptions>>().Value;

                DefaultAzureCredential credential = new(
                                                        new DefaultAzureCredentialOptions
                                                        {
                                                            TenantId = settings.TenantId
                                                        });

                AIProjectClient projectClient = new(
                                                    endpoint:
                                                        new Uri(settings.ProjectEndpoint),
                                                    tokenProvider:
                                                        credential);

                return projectClient.ProjectOpenAIClient.GetProjectResponsesClientForModel(settings.ModelDeployment);
            });

        services.AddSingleton<IProductIdentificationService, FoundryProductIdentificationService>();

        services.AddOptions<ProductKnowledgeSearchOptions>()
    .Bind(configuration.GetSection(ProductKnowledgeSearchOptions.SectionName))
    .Validate(options => Guid.TryParse(options.TenantId, out _), "ProductKnowledgeSearch TenantId must be a valid tenant ID.")
    .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out Uri? endpoint) && endpoint is not null && endpoint.Scheme == Uri.UriSchemeHttps, "ProductKnowledgeSearch Endpoint must be a valid HTTPS URL.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.IndexName), "ProductKnowledgeSearch IndexName is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SemanticConfigurationName), "ProductKnowledgeSearch SemanticConfigurationName is required.")
    .ValidateOnStart();

        services.AddSingleton<SearchClient>(serviceProvider =>
        {
            ProductKnowledgeSearchOptions settings = serviceProvider.GetRequiredService<IOptions<ProductKnowledgeSearchOptions>>().Value;
            DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions { TenantId = settings.TenantId });

            return new SearchClient(new Uri(settings.Endpoint), settings.IndexName, credential);
        });

        services.AddSingleton<IProductKnowledgeService, FoundryProductKnowledgeService>();

        services.AddScoped<IProductAssistantService, FoundryProductAssistantService>();

        return services;
    }
}