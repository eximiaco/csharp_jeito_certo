using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace GymErp.Common;

public sealed class AddTenantHeaderSwagger : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var hdrParameter = new OpenApiParameter()
        {
            Name = "X-Tenant",
            Kind = OpenApiParameterKind.Header,
            IsRequired = true,
            Type = JsonObjectType.String,
            Default = "xyz",
            Description = "The description of the field"
        };
        context.OperationDescription.Operation.Parameters.Add(hdrParameter);

        return true;
    }
}