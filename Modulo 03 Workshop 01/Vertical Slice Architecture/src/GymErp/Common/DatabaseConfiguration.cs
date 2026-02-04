namespace GymErp.Common;

public record DatabaseConfiguration(
    string Host,
    int Port,
    string User,
    string Password,
    string DatabaseName,
    bool DisableSsl,
    bool Pooling,
    int MaxPoolSize,
    int MinPoolSize,
    int Timeout,
    int ConnectionIdleLifetime,
    bool Multiplexing);
