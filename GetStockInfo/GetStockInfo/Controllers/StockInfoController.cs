using GetStockInfo.Domain.Entities;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GetStockInfo.Controllers
{
    [ApiController]
    [Route("StockInfo")]
    public class StockInfoController : ControllerBase
    {
        private readonly LineBotSecret _lineBotSecret;
        private readonly IMemoryCache _cache;
        private readonly string ChannelAccessToken;
        private readonly string UserID;

        public StockInfoController(IOptions<LineBotSecret> lineBotSecret, IMemoryCache cache)
        {
            //IOptions利用繫結class，將設定值configure新增到服務容器在繫結到設定(program)
            _lineBotSecret = lineBotSecret.Value;
            ChannelAccessToken = _lineBotSecret.AccessToken;
            UserID = _lineBotSecret.UserID;
            _cache = cache;
        }

        [HttpGet]
        [Route("GetStockInfo")]
        public async Task<IActionResult> GetStockInfo()
        {
            DateTime date = DateTime.UtcNow;
            if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                date = date.AddDays(-1);
            }
            else if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(-2);
            }
            var stockDate = date.ToString("yyyyMMdd");
            var stockInfoUrl = $"https://www.twse.com.tw/rwd/zh/afterTrading/MI_INDEX20?date={stockDate}&response=json&_=1719673902793";

            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(stockInfoUrl);
                var stockInfo = JsonSerializer.Deserialize<StockInfo>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true, //轉換時不區分大小寫
                });

                if (stockInfo != null)
                {
                    var stockData = stockInfo.Data.Select(data => new StockData
                    {
                        Ranking = Convert.ToInt32(data[0].GetValue()),
                        StockID = data[1].GetValue().ToString(),
                        StockName = data[2].GetValue().ToString(),
                        TradingVolume = data[3].GetValue().ToString(),
                        Transaction = data[4].GetValue().ToString(),
                        OpeningPrice = data[5].GetValue().ToString(),
                        HighestPrice = data[6].GetValue().ToString(),
                        LowestPrice = data[7].GetValue().ToString(),
                        ClosingPrice = data[8].GetValue().ToString(),
                        Difference = data[9].GetValue().ToString(),
                        Change = data[10].GetValue().ToString(),
                        LastBestBidPrice = data[11].GetValue().ToString(),
                        LastBestAskPrice = data[12].GetValue().ToString()
                    }).ToList();

                    var stockInfos = new StockInfo
                    {
                        Stat = stockInfo.Stat,
                        Notes = stockInfo.Notes,
                        Total = stockInfo.Total,
                        Datas = stockData,
                        Fields = stockInfo.Fields,
                        Title = stockInfo.Title,
                    };

                    return Ok(stockInfos);
                }

                return Ok(json);
            }
        }

        [HttpGet]
        [Route("LineBot")]
        public async Task<IActionResult> SendLineBotMsg()
        {
            #region .net core IConfiguration is already been configured
            //var ChannelAccessToken = _configuration.GetSection("LineBot:AccessToken").Value;
            //var UserID = _configuration.GetSection("LineBot:UserID").Value;
            #endregion

            var lineBot = new isRock.LineBot.Bot(ChannelAccessToken);
            lineBot.PushMessage(UserID, "請選擇產品種類");
            var actions = new List<isRock.LineBot.TemplateActionBase>
            {
                new isRock.LineBot.PostbackAction()
                {
                    label = "股票",
                    data = "stock",

                },
                new isRock.LineBot.PostbackAction()
                {
                    label = "基金",
                    data = "fund"
                }
            };
            var buttonTemplate = new isRock.LineBot.ButtonsTemplate()
            {
                altText = "股票、基金資訊", //Line聊天室會顯示的文字
                text = "請點選下方要查詢的種類", //圖片下方文字
                title = "股票、基金", //圖片下方標題
                thumbnailImageUrl = new Uri("https://images.unsplash.com/photo-1526304640581-d334cdbbf45e?q=80&w=2070&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"),
                actions = actions //設定回覆動作
            };
            lineBot.PushMessage(UserID, buttonTemplate);

            return Ok(lineBot);
        }

        //[HttpGet]
        //[Route("StockOptions")]
        //public async Task<IActionResult> StockOptions()
        //{
        //    var lineBot = new isRock.LineBot.Bot(ChannelAccessToken);
        //    lineBot.PushMessage(UserID, "請輸入股票代碼或名稱");

        //    return Ok();
        //}

        //[HttpGet]
        //[Route("FundOptions")]
        //public async Task<IActionResult> FundOptions()
        //{
        //    var lineBot = new isRock.LineBot.Bot(ChannelAccessToken);
        //    lineBot.PushMessage(UserID, "請輸入基金代碼或名稱");

        //    return Ok();
        //}

        [HttpPost]
        [Route("LineBot/Webhook")]
        public async Task<IActionResult> LineBotWebhook([FromBody] LineBotRequest request)
        {
            var lineBot = new isRock.LineBot.Bot(ChannelAccessToken);
            var type = request.events.FirstOrDefault()?.postback?.data;
            if (string.IsNullOrWhiteSpace(type) == false)
            {
                _cache.Set("type", type);
                if (type == "stock")
                {
                    lineBot.PushMessage(UserID, "請輸入股票代碼或名稱");
                }
                else
                {
                    lineBot.PushMessage(UserID, "請輸入基金代碼或名稱");
                }
            }
            else
            {
                _cache.TryGetValue("type", out string preType);
                if (preType == "stock")
                {
                    var recievedMsg = request.events.FirstOrDefault()?.message.text;
                    var stockOptionsUrl = $"https://tw.stock.yahoo.com/_td-stock/api/resource/WaferAutocompleteService;view=wafer&query={recievedMsg}";

                    using (var client = new HttpClient())
                    {
                        var json = await client.GetStringAsync(stockOptionsUrl);
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(json);
                        List<StockOptions> options = new List<StockOptions>();
                        var nodes = doc.DocumentNode.SelectNodes("//li/a");
                        if (nodes != null)
                        {
                            foreach (var node in nodes)
                            {
                                var href = node.GetAttributeValue("href", "").TrimEnd('\\');
                                string stockId = null;
                                string quote = null;
                                var stockOption = new StockOptions();
                                if (href.Contains("stock_id="))
                                {
                                    stockId = href.Split("stock_id=")[1].Split('&')[0];
                                    stockOption.Options.Add(stockId);
                                }
                                if (href.Contains("quote/"))
                                {
                                    quote = href.Split("quote/")[1].Split('&')[0];
                                    stockOption.Options.Add(quote);
                                }
                                options.Add(stockOption);
                            }
                            var actions = new List<isRock.LineBot.TemplateActionBase>();
                            int maxActions = 4; // 最大允许的 actions 数量
                            int currentActionsCount = 0;
                            foreach (var opt in options)
                            {
                                if (currentActionsCount >= maxActions)
                                {
                                    break; // 如果已经达到最大数量，则跳出循环
                                }
                                actions.Add(new isRock.LineBot.PostbackAction()
                                {
                                    label = string.Join(", ", opt.Options),
                                    data = opt.Options.ToString(),
                                });
                                currentActionsCount++; // 更新当前 actions 数量
                            }
                            var buttonTemplate = new isRock.LineBot.ButtonsTemplate()
                            {
                                altText = "請選擇欲查詢的股票", //Line聊天室會顯示的文字
                                text = "請選擇欲查詢的股票", //圖片下方文字
                                title = "股票", //圖片下方標題
                                thumbnailImageUrl = new Uri("https://images.unsplash.com/photo-1526304640581-d334cdbbf45e?q=80&w=2070&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"),
                                actions = actions //設定回覆動作
                            };
                            lineBot.PushMessage(UserID, buttonTemplate);
                            //會再回傳要查詢的股票是哪隻，再丟給她                             
                        }
                    }
                }
                else
                {
                    lineBot.PushMessage(UserID, "請輸入正確的股票代碼或名稱");
                }
            };
            return Ok();

        }
    }
}