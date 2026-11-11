using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Eximia.Integrations.Shared.Postgres;

public class JsonParameter : SqlMapper.ICustomQueryParameter
{
    private readonly string _value;

    public JsonParameter(string value)
    {
        _value = value;
    }

    public void AddParameter(IDbCommand command, string name)
    {
        var parameter = new NpgsqlParameter(name, NpgsqlDbType.Json);
        parameter.Value = _value;

        command.Parameters.Add(parameter);
    }
}   
