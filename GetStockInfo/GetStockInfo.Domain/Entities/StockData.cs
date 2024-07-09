using System.ComponentModel;
using System.Text.Json;

namespace GetStockInfo.Domain.Entities
{
#nullable disable
    public class StockData
    {
        [Description("排名")]
        public int Ranking { get; set; }
        [Description("證卷代號")]
        public string StockID { get; set; }
        [Description("證卷名稱")]
        public string StockName { get; set; }
        [Description("成交股數")]
        public string TradingVolume { get; set; }
        [Description("成交筆數")]
        public string Transaction { get; set; }
        [Description("開盤價")]
        public string OpeningPrice { get; set; }
        [Description("最高價")]
        public string HighestPrice { get; set; }
        [Description("最低價")]
        public string LowestPrice { get; set; }
        [Description("收盤價")]
        public string ClosingPrice { get; set; }
        [Description("漲跌")]
        public string Difference { get; set; }
        [Description("漲跌價差")]
        public string Change { get; set; }
        [Description("最後揭示買價")]
        public string LastBestBidPrice { get; set; }
        [Description("最後揭示賣價")]
        public string LastBestAskPrice { get; set; }
    }

    public class StockOptions
    {
        public List<string> Options { get; set; } = new List<string>();
    }

    public class StockInfo
    {
        public string Stat { get; set; }
        public string Date { get; set; }
        public string Title { get; set; }
        public List<string> Fields { get; set; }
        public List<StockData> Datas { get; set; }
        public List<List<object>> Data { get; set; }
        public List<string> Notes { get; set; }
        public int Total { get; set; }
    }

    public static class JsonElementExtensions
    {
        public static object GetValue(this object obj)
        {
            if (obj is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        return jsonElement.GetString();
                    case JsonValueKind.Number:
                        return jsonElement.GetInt32();
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return jsonElement.GetBoolean();
                    default:
                        return jsonElement.GetRawText();
                }
            }
            return obj;
        }
    }
}