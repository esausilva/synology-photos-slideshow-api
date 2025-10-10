namespace Synology.Photos.Slideshow.Api.DI;

public static class ApiDependencyConfigurationExtensions
{
    public static ConfigurationManager GetApiConfigurations(this ConfigurationManager configuration)
    {
        configuration.SetBasePath(Directory.GetCurrentDirectory());
        configuration.AddUserSecrets(typeof(Program).Assembly)
            .AddEnvironmentVariables();
        
#if DEBUG
        Console.WriteLine(configuration.GetDebugView());
#endif
        
        return configuration;
    }
}