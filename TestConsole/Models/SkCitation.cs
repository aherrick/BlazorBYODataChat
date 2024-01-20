namespace TestConsole.Models;

public class SkCitation
{
    // ToolContent
    public class Rootobject
    {
        public Citation[] citations { get; set; }
        public string intent { get; set; }
    }

    public class Citation
    {
        public string content { get; set; }
        public object id { get; set; }
        public object title { get; set; }
        public object filepath { get; set; }
        public object url { get; set; }
        public Metadata metadata { get; set; }
        public string chunk_id { get; set; }
    }

    public class Metadata
    {
        public string chunking { get; set; }
    }
}