namespace GetStockInfo.Domain.Entities
{
    public class LineBotSecret
    {
        public const string LineBot = "LineBot";
        public string AccessToken { get; set; }
        public string UserID { get; set; }
    }
}