using HtmlAgilityPack;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ETHDINFKBot
{
    public class GoogleEngine
    {

        public GoogleEngine()
        {
            var httpClientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };
            HttpClient = new HttpClient(httpClientHandler);

            //{Method: GET, RequestUri: 'https://consent.google.com/ml?continue=https://www.google.com/&gl=CH&m=0&pc=shp&uxe=none&hl=de&src=1', Version: 1.1, Content: <null>, Headers:
            var response = HttpClient.GetAsync("https://google.com").Result;
        }

        public bool cacheResponses = true; //Indicates If A Cache Of All Search Results Should Be Saved To Increase Speed And Lower Actual Queries
        public HttpClient HttpClient;// = new HttpClient(); //WebClient Used To Fetch Google Search Results
        public Dictionary<string, string[]> cache = new Dictionary<string, string[]>(); //Cache Dictionary Containtion All Results Cached IMAGES ONLY

        //Used To Send A Search Query
        public async Task<SearchResult> Search(string query, int start = 0)
        {
            //Try To Load The Cache If Caching Is Enabled And A Cache Has Not Been Loaded Yet
            //try { if (cacheResponses && cache.Count == 0) LoadCache(); } catch { }

            try
            {
                string queryUrl = "https://www.google.com/search?q=" + HttpUtility.UrlEncode(query.ToLower()) + "&start=" + start + "&hl=en&gl=en&safe=active"; //Create The Query URL
                                                                                                                                                                //if (cache.ContainsKey(queryUrl)) //Check If This Query Has Already Been Sent
                SearchResult searchResult = new SearchResult();

                var response = await HttpClient.GetAsync(queryUrl); //Download The HTML From The Query URL
                var responseString = await response.Content.ReadAsStringAsync();

                var doc = new HtmlAgilityPack.HtmlDocument(); //Create An HTML Document From The Downloaded HTML
                doc.LoadHtml(responseString); //Load The Downloaded HTML
                var divs = doc.DocumentNode.Descendants("div"); //Get All The Divs In The Document
                List<Result> results = new List<Result>(); //Create A List For All The Results
                foreach (HtmlNode node in divs) //Loop Through All Nodes In The Div Array
                {
                    if (node.GetClasses().Contains("ZINbbc")) //Check If The Node Has The Class "ZINbbc"
                    {


                        if (node.FirstChild.GetClasses().Contains("kCrYT")) //Check If The Node Has The Class "jfp3ef"
                        {
                            if (node.FirstChild.FirstChild.OuterHtml.StartsWith("<a href=\"/url?q=")) //Check If The Link(Inside The Node)'s Outer HTML Starts With "<a href="/url?q="
                            {
                                results.Add(new Result() //Add A New Result Class
                                {
                                    url = HttpUtility.HtmlDecode(node.FirstChild.FirstChild.GetAttributeValue("href", "").Substring(("/url?q=").Length).Split('&')[0]), //Set The URL To The One Found In The Link (Inside The Node)
                                    title = HttpUtility.HtmlDecode(node.FirstChild.FirstChild.FirstChild.InnerText), //Set The Result Title
                                    description = HttpUtility.HtmlDecode(node.LastChild.InnerText) //Set The Result Description
                                });
                            }
                            else
                            {
                                if (searchResult.Description == null)
                                {
                                    string desc = node.InnerText;

                                    var imgNode = node.Descendants("img").FirstOrDefault();
                                    if (imgNode != null)
                                    {
                                        string id = imgNode.Id;




                                        string startString = "data:image";
                                        string endString = $"['{id}']";
                                        int endIdx = responseString.IndexOf(endString) - 8;

                                        string temp = responseString.Substring(0, endIdx);// workaround as last index of
                                        int pTo = temp.LastIndexOf(startString);
                                        temp = temp.Substring(pTo);

                                        searchResult.Description = desc;
                                        searchResult.ImageUrl = temp;
                                    }
                                }
                                else
                                {

                                }

                            }
                        }

                    }
                }

                //Save Cache If Enabled And The Response HTML Was Longer Than 1500 Characters (If It's Longer Than That, It Has Probably Gone Well)
                if (responseString.Length > 1500 && cacheResponses)
                {
                    //cache[queryUrl] = results.ToArray(); //Set The Cache
                    //SaveCache(); //Save The Cache
                }


                // TODO Restore normal google search

                //searchResult.Results = results;
                return searchResult;
            }
            catch (Exception ex)
            {
                return null; //Return Null On Error
            }
        }

        public async Task<List<string>> GetSearchResultBySelenium(string query, int start = 0, string lang = "en")
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--headless");
            options.AddArguments("--disable-gpu");

            ChromeDriver driver;

            #if DEBUG
            driver = new ChromeDriver(options);
#elif
            driver = new ChromeDriver(Program.ApplicationSetting.BasePath, options);
#endif
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(1000);
            driver.Navigate().GoToUrl("https://images.google.com/?hl={lang}&gl={lang}&safe=active");

            var buttons = driver.FindElements(By.TagName("button"));

            foreach (var button in buttons)
            {
                // Find the accept all cookies button
                // TODO English text check
                if (button.Text.ToLower().StartsWith("alle") || button.Text.ToLower().StartsWith("accept"))
                {
                    button.Click();
                }
            }

            var title = driver.Title;

            var inputs = driver.FindElements(By.TagName("input"));
            foreach (var input in inputs)
            {
                try
                {
                    var inputLabel = ((OpenQA.Selenium.WebElement)(input)).ComputedAccessibleLabel;
                    // Find the accept all cookies button
                    // TODO English text check
                    if (inputLabel.ToLower().StartsWith("suche") || inputLabel.ToLower().StartsWith("search"))
                    {
                        ///button.Click();
                        input.SendKeys(query);
                        input.SendKeys(Keys.Return);
                    }
                }
                catch (Exception ex)
                {
                }
            }

            var images = driver.FindElements(By.XPath("//a[@href]"));

            var root = (OpenQA.Selenium.WebElement)(driver.FindElement(By.XPath("html")));
            var rootHtml = root.GetAttribute("innerHTML");

            bool googleImages = true;
            List<string> imageUrls = new List<string>();
            int maxCount = 5;
            foreach (var image in images)
            {
                try
                {
                    // TODO Check if this filter is correct
                    if (image.GetAttribute("href").Contains("google"))
                        continue;

                    /*if (image.GetAttribute("href").Contains("imgurl"))
                        imageUrls.Add("");*/

                    if (imageUrls.Count < maxCount)
                    {
                        //image.Click();
                        WebElement parent = (OpenQA.Selenium.WebElement)image.FindElement(By.XPath("../a"));
                        WebElement parent2 = (OpenQA.Selenium.WebElement)image.FindElement(By.XPath("../.."));
                        WebElement parent3 = (OpenQA.Selenium.WebElement)image.FindElement(By.XPath("../../.."));

                        var parentHref = parent.GetAttribute("href");
                        var parentHref2 = parent2.GetAttribute("href");
                        var parentHref3 = parent3.GetAttribute("href");

                        var innerHtml = parent.GetAttribute("innerHTML");
                        var innerHtml2 = parent2.GetAttribute("innerHTML");
                        var innerHtml3 = parent3.GetAttribute("innerHTML");

                        var imageHtml = image.GetAttribute("innerHTML");

                        parent.Click();
                        var currentTitle = driver.Title;
                        if(!currentTitle.Contains("Google Search"))
                        {
                            driver.Close();
                        }
                        System.Threading.Thread.Sleep(1000);

                        var newImageLinks = driver.FindElements(By.XPath("//a[@href]"));
                        foreach (var newImageLink in newImageLinks)
                        {
                            string url = newImageLink.GetAttribute("href");
                            //Console.WriteLine(url);
                            if (url.Contains("imgurl")/*/ && image.GetAttribute("href").EndsWith(".jpg") && image.GetAttribute("href").EndsWith(".png")*/)
                            {
                                Uri myUriCustom = new Uri(url);

                                if (myUriCustom.Host == "")
                                {
                                    url = "https://example.com" + url;
                                    myUriCustom = new Uri(url);
                                }
                                var newLink = HttpUtility.ParseQueryString(myUriCustom.Query).Get("imgurl");
                                if (!imageUrls.Contains(newLink))
                                    imageUrls.Add(newLink);
                            }
                        }

                        //var candidateLinks = newImageLinks.Where(i => i.GetAttribute("href").Contains("imgurl")).Select(i => i.GetAttribute("href"));

                        if (parentHref == null)
                            continue;
                        Uri myUri = new Uri(parentHref);

                        if (myUri.Host == "")
                        {
                            parentHref = "https://example.com" + parentHref;
                            myUri = new Uri(parentHref);
                        }

                        var newLinkDirect = HttpUtility.ParseQueryString(myUri.Query).Get("imgurl");
                        if (!imageUrls.Contains(newLinkDirect) && string.IsNullOrWhiteSpace(newLinkDirect))
                            imageUrls.Add(newLinkDirect);
                    }
                }
                catch (Exception ex)
                {
                }
            }

            //var links = images.Select(i => i.GetAttribute("href")).ToList();

            driver.Quit();


            return imageUrls;
        }

        public async Task<List<string>> ImageSearch(string query, int start = 0, string lang = "en")
        {
            //Try To Load The Cache If Caching Is Enabled And A Cache Has Not Been Loaded Yet
            try
            {
                if (cacheResponses && cache.Count == 0)
                    LoadCache();
            }
            catch (Exception ex)
            {
            }

            try
            {

                if (cache.ContainsKey(query))
                {
                    // TODO Dont return the first see first non empty
                    if (!string.IsNullOrWhiteSpace(cache[query][0]))
                        return cache[query].ToList() ?? new List<string>();
                }

                // TODO Do a central setup
                HttpClient.DefaultRequestHeaders.Clear();
                HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36");

                HttpClient.DefaultRequestHeaders.Accept.Clear();
                HttpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));
                HttpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
                HttpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));

                string queryUrl = $"https://www.google.com/search?q={HttpUtility.UrlEncode(query.ToLower())}&start={start}&hl={lang}&gl={lang}&safe=active"; //Create The Query URL
                                                                                                                                                             //if (cache.ContainsKey(queryUrl)) //Check If This Query Has Already Been Sent
                SearchResult searchResult = new SearchResult();

                var response = await HttpClient.GetAsync(queryUrl); //Download The HTML From The Query URL
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.RequestMessage.RequestUri.ToString().Contains("sorry"))
                {
                    // Likely a ratelimit was hit
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
                    return new List<string>();
                }

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

                var doc = new HtmlAgilityPack.HtmlDocument(); //Create An HTML Document From The Downloaded HTML
                doc.LoadHtml(responseString); //Load The Downloaded HTML
                var divs = doc.DocumentNode.Descendants("div"); //Get All The Divs In The Document
                List<Result> results = new List<Result>(); //Create A List For All The Results

                var imageDiv = doc.DocumentNode.SelectNodes("//*[@class=\"idg8be\"]");
                if (imageDiv == null)
                {
                    // Fallback no specific div found
                    var imgNodes = doc.DocumentNode.SelectNodes("//img");

                    return new List<string>();
                }


                var hrefs = imageDiv[0].SelectNodes("a").Select(i => i.GetAttributeValue("href", string.Empty));
                string validImage = "";

                if (hrefs.Count() == 0)
                    return new List<string>(); // only if more than one was found proceed

                var links = new List<string>();

                foreach (var link in hrefs)
                {
                    try
                    {
                        Uri myUri = new Uri(link);

                        if (myUri.Host == "")
                        {
                            string newLink = "https://example.com" + link;
                            myUri = new Uri(newLink);
                        }

                        string param1 = HttpUtility.ParseQueryString(myUri.Query).Get("imgurl");

                        if (!string.IsNullOrWhiteSpace(param1))
                            links.Add(param1 ?? "");
                    }
                    catch (Exception ex)
                    {
                        // TODO See in which cases this happens 
                    }
                }

                if (links.Count == 0)
                    return new List<string>(); // No imgs found

                //Save Cache If Enabled And The Response HTML Was Longer Than 1500 Characters (If It's Longer Than That, It Has Probably Gone Well)
                if (responseString.Length > 1500 && cacheResponses)
                {
                    if (cache.ContainsKey(query))
                        cache.Remove(query);

                    cache.Add(query, links.ToArray()); //Set The Cache
                    SaveCache(); //Save The Cache
                }

                return links;

                /*
                foreach (HtmlNode node in divs) //Loop Through All Nodes In The Div Array
                {
                    if (node.Attributes.Count == 1 && node.Attributes[0].Value == "ZINbbc xpd O9g5cc uUPGi") //Check If The Node Has The Class "ZINbbc"
                    {


                        if (node.FirstChild.GetClasses().Contains("kCrYT")) //Check If The Node Has The Class "jfp3ef"
                        {
                            if (node.FirstChild.FirstChild.OuterHtml.StartsWith("<a href=\"/url?q=")) //Check If The Link(Inside The Node)'s Outer HTML Starts With "<a href="/url?q="
                            {
                                results.Add(new Result() //Add A New Result Class
                                {
                                    url = HttpUtility.HtmlDecode(node.FirstChild.FirstChild.GetAttributeValue("href", "").Substring(("/url?q=").Length).Split('&')[0]), //Set The URL To The One Found In The Link (Inside The Node)
                                    title = HttpUtility.HtmlDecode(node.FirstChild.FirstChild.FirstChild.InnerText), //Set The Result Title
                                    description = HttpUtility.HtmlDecode(node.LastChild.InnerText) //Set The Result Description
                                });
                            }
                            else
                            {
                                if (searchResult.Description == null)
                                {
                                    string desc = node.InnerText;

                                    var imgNode = node.Descendants("img").FirstOrDefault();
                                    if (imgNode != null)
                                    {
                                        string id = imgNode.Id;




                                        string startString = "data:image";
                                        string endString = $"['{id}']";
                                        int endIdx = response.IndexOf(endString) - 8;

                                        string temp = response.Substring(0, endIdx);// workaround as last index of
                                        int pTo = temp.LastIndexOf(startString);
                                        temp = temp.Substring(pTo);

                                        searchResult.Description = desc;
                                        searchResult.ImageUrl = temp;
                                    }
                                }
                                else
                                {

                                }

                            }
                        }

                    }
                }






                searchResult.Results = results;
                return searchResult;*/
            }
            catch (Exception ex)
            {
                return new List<string>(); //Return Null On Error
            }
            return new List<string>();
        }

        //Search Multiple Pages
        public Result[] SearchPages(string query, int pages)
        {
            /*List<Result> results = new List<Result>(); //List To Save Found Results
            for (int i = 0; i < pages; i++) //Loop The Amount Of Times Specified By The Pages INT
                results.AddRange(Search(query, results.Count)); //Add Results Found
            return results.ToArray(); //Return The List As An Array
            */
            return null;
        }

        public void LoadCache()
        {
            if (File.Exists(Path.Combine(Program.ApplicationSetting.BasePath, "GoogleImageSearchCache.json")))
                cache = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(Program.ApplicationSetting.BasePath, "GoogleImageSearchCache.json")));
        }
        //Read The Cache File And Deserialize It Into A Dictionary

        public void SaveCache()
        {
            File.WriteAllText(Path.Combine(Program.ApplicationSetting.BasePath, "GoogleImageSearchCache.json"), JsonConvert.SerializeObject(cache, Formatting.Indented));
        }
        //Serialize The Cache Dictionary And Save It To The Cache File


        public class Result { public string url, title, description; }
        public class SearchResult
        {
            public string ImageUrl { get; set; }
            public string Description { get; set; }
            public List<string> Results { get; set; }
        }

        //Simple Class To Store Result URL, Title And Description
        //👌👌👌👌👌👌👌👌👌
    }
}
