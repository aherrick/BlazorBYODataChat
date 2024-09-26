namespace Server.Models;

public class Configuration
{
    public class AzureOpenAI
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
    }

    public class AzureOpenAIChat : AzureOpenAI
    {
        public string DeploymentName { get; set; }
    }

    public class AzureOpenAITextEmbedding : AzureOpenAI
    {
        public string DeploymentName { get; set; }
    }

    public class AzureAISearch
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string IndexName { get; set; }
        public bool InScope { get; set; }
    }
}