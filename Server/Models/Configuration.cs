namespace Server.Models;

public class Configuration
{
    public class AzureOpenAI
    {
        public string DeploymentName { get; set; }
        public string Endpoint { get; set; }
        public string Key { get; set; }
    }

    public class AzureAISearch
    {
        public string DeploymentName { get; set; }
        public string ModelName { get; set; }
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string IndexName { get; set; }
    }
}