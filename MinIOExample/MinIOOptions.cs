namespace MinIOExample;

public sealed class MinIOOptions
{
    public string SecretKey { get; init; } = string.Empty;

    public string AccessKey { get; init; } = string.Empty;

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 0;

    public MinIOOptions()
    {
    }
}
