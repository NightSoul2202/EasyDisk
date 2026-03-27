namespace EasyDisk.API.Exceptions
{
    public class MissingConfigurationException : ApiException
    {
        public MissingConfigurationException(string settingName) 
            : base($"Critical error on start: Missing configuration {settingName}")
        {
        }
    }
}
