using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using System.Text.Json;

namespace LostArkMarketWatcherV2.modules
{
    public static class LamoApi
    {
        public static List<Cookie> GetCookies(this HttpResponseMessage message)
        {
            message.Headers.TryGetValues("Set-Cookie", out var cookiesHeader);
            var cookies = cookiesHeader!.Select(cookieString => CreateCookie(cookieString)).ToList();
            return cookies!;
        }

        private static Cookie CreateCookie(string cookieString)
        {
            var properties = cookieString.Split(';', StringSplitOptions.TrimEntries);
            var name = properties[0].Split("=")[0];
            var value = properties[0].Split("=")[1];
            var path = properties[1].Replace("path=", "");
            var cookie = new Cookie(name, value, path)
            {
                Secure = properties.Contains("secure"),
                HttpOnly = properties.Contains("httponly"),
            };
            return cookie;
        }
        public static async Task login(string email, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                LamoLogger logger = (System.Windows.Application.Current as LamoWatcherApp)?.logger!;

                var requestContent = new StringContent($"{{\"email\": \"{email}\", \"password\": \"{password}\"}}", Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                logger?.Debug($"[POST] {LamoConfig.Instance.baseUrl}/api/auth/login");
                HttpResponseMessage response = await client.PostAsync($"{LamoConfig.Instance.baseUrl}/api/auth/login", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    Cookie cookie = GetCookies(response)[0];
                    LamoConfig.Instance.cookie = cookie;
                    return;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }
        public static async Task session()
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                    LamoLogger logger = (System.Windows.Application.Current as LamoWatcherApp)?.logger!;
                    if (LamoConfig.Instance.cookie != null)
                    {
                        handler.CookieContainer.Add(new Uri(LamoConfig.Instance.baseUrl), LamoConfig.Instance.cookie);
                    }

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                    logger?.Debug($"[GET] {LamoConfig.Instance.baseUrl}/api/auth/sessions");
                    HttpResponseMessage response = await client.GetAsync($"{LamoConfig.Instance.baseUrl}/api/auth/sessions");

                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                    else
                    {
                        throw new Exception(response.ReasonPhrase);
                    }
                }
            }
        }

        public static async Task Upload(MarketLine marketLine)
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                    LamoLogger logger = (System.Windows.Application.Current as LamoWatcherApp)?.logger!;
                    if (LamoConfig.Instance.cookie != null)
                    {
                        handler.CookieContainer.Add(new Uri(LamoConfig.Instance.baseUrl), LamoConfig.Instance.cookie);
                    }

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                    Dictionary<string, dynamic> body = new()
                    {
                        { "itemId", marketLine.ItemId },
                        { "lowPrice", marketLine.LowestPrice },
                        { "cheapestRemaining", marketLine.CheapestRemaining },
                        { "recentPrice", marketLine.RecentPrice },
                        { "avgPrice", marketLine.AvgPrice },
                        { "jumpstart", LamoConfig.Instance.jumpstart },
                        { "regionId", LamoConfig.Instance.region! },
                        { "watcherVersion", LamoConfig.Instance.version }
                    };
                    string bodyString = JsonSerializer.Serialize(body);
                    var requestContent = new StringContent(bodyString, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = await client.PostAsync($"{LamoConfig.Instance.baseUrl}/api/watcher/push-entry", requestContent);

                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                    else
                    {
                        throw new Exception(response.ReasonPhrase);
                    }
                }
            }
        }
    }
}
