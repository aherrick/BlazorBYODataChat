namespace Server.Models.Sk;

public class SkResponseInnerContent
{
    public class Rootobject
    {
        public Message[] messages { get; set; }
        public int index { get; set; }
    }

    public class Message
    {
        public Delta delta { get; set; }
        public bool end_turn { get; set; }
    }

    public class Delta
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}