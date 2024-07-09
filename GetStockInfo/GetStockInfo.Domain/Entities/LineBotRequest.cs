namespace GetStockInfo.Domain.Entities
{
    public class LineBotRequest
    {
        public List<Event> events { get; set; }
    }

    public class Event
    {
        public string type { get; set; }
        public Source source { get; set; }
        public Message message { get; set; }
        public Postback postback { get; set; }
        public string replyToken { get; set; }
    }

    public class Source
    {
        public string userId { get; set; }
    }

    public class Message
    {
        public string type { get; set; }
        public string text { get; set; }
    }

    public class Postback
    {
        public string data { get; set; }
    }
}