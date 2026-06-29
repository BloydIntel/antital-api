using Antital.Application.DTOs.Investments;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Antital.API.Configs;

public class PaystackWebhookOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.Name != "PaystackWebhook")
        {
            return;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(PaystackWebhookPayloadDto), context.SchemaRepository),
                    Example = new OpenApiObject
                    {
                        ["event"] = new OpenApiString("charge.success"),
                        ["data"] = new OpenApiObject
                        {
                            ["reference"] = new OpenApiString("ANT-ORD-42-a1b2c3d4e5f6"),
                            ["amount"] = new OpenApiInteger(102_500),
                            ["channel"] = new OpenApiString("card"),
                            ["status"] = new OpenApiString("success"),
                        },
                    },
                },
            },
        };

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "x-paystack-signature",
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "HMAC SHA512 signature of the raw request body.",
        });
    }
}
