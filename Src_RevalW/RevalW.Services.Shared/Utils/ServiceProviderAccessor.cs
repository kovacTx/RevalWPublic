namespace RevalW.Services.Shared.Utils;

public static class ServiceProviderAccessor
{
    public static IServiceProvider Instance { get; private set; } = default!;

    public static void Configure(IServiceProvider provider) => Instance = provider;
}