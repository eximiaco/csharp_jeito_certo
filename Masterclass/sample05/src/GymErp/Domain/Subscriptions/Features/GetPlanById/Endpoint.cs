using FastEndpoints;
using GymErp.Domain.Subscriptions.Aggreates.Plans;
using GymErp.Domain.Subscriptions.Features.GetPlanById;
using GymErp.Domain.Subscriptions.Infrastructure;
using GymErp.Common;
using GymErp.Tenant;
using Dapper;

namespace GymErp.Domain.Subscriptions.Features.GetPlanById;

public class Endpoint : Endpoint<Request, Response>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public Endpoint(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override void Configure()
    {
        Get("/api/plans/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        using var connection = await _connectionFactory.Create(ct);
        await connection.OpenAsync(ct);

        const string sql = @"
            SELECT Id, Description, Type 
            FROM Plans 
            WHERE Id = @Id";

        var parameters = new { Id = req.Id };
        
        var plan = await connection.QueryFirstOrDefaultAsync<Response>(sql, parameters);
        
        if (plan == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(plan, ct);
    }
}
