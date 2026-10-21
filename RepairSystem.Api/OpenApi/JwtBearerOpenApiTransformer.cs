using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace RepairSystem.Api.OpenApi;

internal sealed class JwtBearerOpenApiTransformer :
    IOpenApiDocumentTransformer,
    IOpenApiOperationTransformer
{
    // Tell Swagger how our API uses JWT Bearer tokens.
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes =
            new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT",
                    Description = "Enter the JWT access token."
                }
            };

        return Task.CompletedTask;
    }

    // Tell Swagger which endpoints need the token.
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata =
            context.Description.ActionDescriptor.EndpointMetadata;

        var requiresAuthorization =
            metadata.OfType<IAuthorizeData>().Any();

        var allowsAnonymous =
            metadata.OfType<IAllowAnonymous>().Any();

        if (!requiresAuthorization || allowsAnonymous)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer", context.Document)] = []
        });

        return Task.CompletedTask;
    }
}