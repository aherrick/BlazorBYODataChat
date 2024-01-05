# BlazorBYODataChat
### Bring Your Own Data with Semantic Kernel + Azure AI Search ###
 
![dotnet Ubuntu](https://github.com/aherrick/BlazorBYODataChat/actions/workflows/dotnet.yml/badge.svg)

#### User Secrets for Server Project: ####

```
{
  "AzureOpenAI:DeploymentName": "...",
  "AzureOpenAI:ModelName": "...",
  "AzureOpenAI:Endpoint": "https://....openai.azure.com/",
  "AzureOpenAI:Key": "...",

  "AzureAISearch:DeploymentName": "text-embedding-ada-002",
  "AzureAISearch:ModelName": "text-embedding-ada-002",
  "AzureAISearch:Endpoint": "https://....search.windows.net",
  "AzureAISearch:Key": "...",
  "AzureAISearch:IndexName": "contoso"
}
```

Ingest Support: 

* .txt
* .pdf
