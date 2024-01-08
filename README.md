# BlazorBYODataChat
### Bring Your Own Data with Semantic Kernel + Azure AI Search ###
 
![dotnet Ubuntu](https://github.com/aherrick/BlazorBYODataChat/actions/workflows/dotnet.yml/badge.svg)

#### User Secrets for Server Project: ####

```
{
  "AzureOpenAI": {
    "DeploymentName": "...",
    "ModelName": "...",
    "Endpoint": "https://....openai.azure.com/",
    "Key": "..."
  },

  "AzureAISearch": {
    "DeploymentName": "text-embedding-ada-002",
    "ModelName": "text-embedding-ada-002",
    "Endpoint": "https://....search.windows.net",
    "Key": "...",
    "IndexName": "contoso"
  }
}
```

Ingest Support: 

* .txt
* .pdf
_assests

<img src="https://github.com/aherrick/BlazorBYODataChat/blob/main/_assests/demo.gif" width="75%" />


