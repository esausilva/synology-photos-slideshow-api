namespace Synology.Photos.Slideshow.Api.DI;

public static class ApiDependencyConfigurationExtensions
{
    extension(ConfigurationManager configuration)
    {
        public ConfigurationManager GetApiConfigurations()
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
    
}