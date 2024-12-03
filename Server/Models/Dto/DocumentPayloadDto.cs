namespace Server.Models.Dto;

public class DocumentPayloadDto
{
    public string url { get; set; }
    public string schema { get; set; }
    public string file { get; set; }
    public string text { get; set; }
    public string vector_provider { get; set; }
    public string vector_generator { get; set; }
    public DateTime last_update { get; set; }
}