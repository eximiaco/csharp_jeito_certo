namespace Eximia.Integrations.Outbox;

public static class OutboxDefaults
{
    /// <summary>Nome da tabela do outbox do Silverback (padrão do Silverback.Storage.PostgreSql).</summary>
    public const string TableName = "SilverbackOutbox";

    /// <summary>Nome do advisory lock que garante um único worker de outbox ativo.</summary>
    public const string LockName = "eximia-outbox-worker";

    public const string HeaderIntegrationId = "x-integration-id";
}
