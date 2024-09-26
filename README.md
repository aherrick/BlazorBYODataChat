# BlazorBYODataChat
### Bring Your Own Data with Semantic Kernel + Azure AI Search ###
 
![dotnet Ubuntu](https://github.com/aherrick/BlazorBYODataChat/actions/workflows/dotnet.yml/badge.svg)

#### User Secrets for Server Project: ####

```
{
  {
  "AzureOpenAIChat": {
    "DeploymentName": "gpt-4o",
    "Endpoint": "https://....openai.azure.com/",
    "Key": "..."
  },

  "AzureOpenAITextEmbedding": {
    "DeploymentName": "text-embedding-ada-002",
    "Endpoint": "https://....openai.azure.com/",
    "Key": "..."
  },

  "AzureAISearch": {
    "Key": "...",
    "Endpoint": "https://....search.windows.net",
    "IndexName": "MyIndex",
    "InScope": true
  }
}
```

Ingest Support: 

* .txt
* .pdf
* .docx

<img src="https://github.com/aherrick/BlazorBYODataChat/blob/main/_assests/demo.gif" width="85%" />


