using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Helpers
{
    public enum Restaurant
    {
        ETH_Polymensa = 0,
        UZH_UpperMensa = 1,
        UZH_LowerMensa = 2,
        UZH_LowerMensa_Dinner = 3,
        UZH_Lichthof_Rondel = 4
    }
    public enum Location
    {
        Zentrum = 0,
        Hoengg = 1
    }

    public enum MealTime
    {
        Breakfast = 0,
        Lunch = 1,
        Dinner = 2
    }
    public enum Language
    {
        English = 0,
        German = 1
    }

    public class Menu
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string MultilineDescription { get; set; }
        public string FirstLine { get; set; }
        public decimal Price { get; set; }
        public bool IsVegan { get; set; }
        public bool IsLocal { get; set; }
        public string ImgUrl { get; set; }
    }

    public static class FoodHelper
    {
        private static GoogleEngine Google = new GoogleEngine();



        public static string ToFriendlyString(this Restaurant restaurant)
        {
            switch (restaurant)
            {
                case Restaurant.ETH_Polymensa:
                    return "12";
                case Restaurant.UZH_UpperMensa:
                    return "zentrum-mensa";
                case Restaurant.UZH_LowerMensa:
                    return "zentrum-mercato";
                case Restaurant.UZH_LowerMensa_Dinner:
                    return "zentrum-mercato-abend";
                case Restaurant.UZH_Lichthof_Rondel:
                    return "lichthof-rondell";
                default:
                    return "unknown";
            }
        }



        // TODO Provide list of ids to search
        private static List<Menu> GetPolymensaMenus(MealTime mealTime = MealTime.Lunch, Language language = Language.English, int id = 12)
        {
            return null;
        }

        private static List<Menu> GetPolymensaMenu(MealTime mealTime = MealTime.Lunch)
        {
            string xPath = "//*[@class=\"scrollarea-content\"]";

            string lang = "en";
            string dateString =  DateTime.Today.ToString("yyyy-MM-dd"); //"2022-03-25";

            int polyMensaId = 12;

            WebClient client = new WebClient();
            string html = client.DownloadString($"https://ethz.ch/en/campus/getting-to-know/cafes-restaurants-shops/gastronomy/menueplaene/offerDay.html?language={lang}&id={polyMensaId}&date={dateString}");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(xPath);

            var mainNode = nodes[0];
            if (mealTime == MealTime.Dinner)
                mainNode = nodes[1];

            var node = mainNode.ChildNodes.First(i => i.Name == "table");

            var childs = node.ChildNodes;

            List<Menu> polymensaMenus = new List<Menu>();
            foreach (var child in childs.Where(i => i.Name == "tr"))
            {
                var childNodes = child.ChildNodes.Where(i => i.Name == "td").ToList();


                var menu = new Menu()
                {
                    Description = childNodes[1].InnerText,
                    Name = childNodes[0].InnerText,
                    Price = decimal.Parse(childNodes[2].InnerText) // TODO fix formating with the comma
                };


                var lines = childNodes[1].ChildNodes.Where(i => i.Name == "strong" || i.Name == "#text").Select(i => i.InnerText.Replace("\"", "")).ToList();
                menu.Description = string.Join(" ", lines);
                menu.MultilineDescription = string.Join("\r\n", lines);// TODO check if atleast 2 lines to begin with
                menu.FirstLine = lines.First(); // TODO check if atleast 2 lines to begin with

                polymensaMenus.Add(menu);
            }

            foreach (var menu in polymensaMenus)
            {
                //break;
                //await Context.Channel.SendMessageAsync($"**Menu: {menu.Description} Description: {menu.Name} IsVegan:{menu.IsVegan} IsLocal:{menu.IsLocal} Price: {menu.Price}**");

                //var reply = new GoogleEngine().ImageSearch(menu.FirstLine.Replace("\"", "").Trim(), lang: "de");




                menu.ImgUrl = GetImageFromGoole(menu.FirstLine, "en");
                if(menu.ImgUrl == "")
                    menu.ImgUrl = GetImageFromGoole(menu.Description, "en");

                if (menu.ImgUrl == "")
                    menu.ImgUrl = "https://cdn.discordapp.com/avatars/153929916977643521/5d6e05d48ab1b0599aa801ac4aebc1ea.png";


            }

            return polymensaMenus;
        }

        private static List<Menu> GetUzhMenus(string day, string mensa, Language language = Language.English)
        {
            ///html/body/div[6]/section/div/section/div[2]/div[1]/div/div[2]/div/div/div/div/div/div/div/div/table/tbody[2]
            string xPath = "//*[@id=\"box-1\"]/ul/li/div/div/div";

            // day = "freitag";
            //string mensa = "zentrum-mercato";


            WebClient client = new WebClient();
            string html = client.DownloadString($"https://www.mensa.uzh.ch/de/menueplaene/{mensa}/{day}.html");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNode node = doc.DocumentNode.SelectSingleNode(xPath);


            List<Menu> menus = new List<Menu>();
            Menu currentMenu = new Menu();

            int step = 0;
            bool brHitOnce = false;
            foreach (var child in node.ChildNodes)
            {
                switch (step)
                {
                    case 0:
                        // Detect Type/Price
                        if (child.Name == "h3")
                        {
                            currentMenu.Name = child.InnerText.Split('|')[0];
                            // Sometimes the price is missing idk why
                            currentMenu.Price = child.InnerText.Contains("|") ? decimal.Parse(child.InnerText.Split('|')[1].Split('/')[0].Replace("CHF","")) : 0;
                            // TODO Detect pricing
                            step++;
                        }
                        else
                        {

                        }

                        break;
                    case 1:
                        // Detect Description
                        if (child.Name == "p")
                        {
                            // !i.Contains(":") to remove the not needed country of origin field 
                            var lines = child.InnerHtml.Trim().Split("<br>", StringSplitOptions.RemoveEmptyEntries).Where(i => !i.Contains(":")).ToList();
                            currentMenu.Description = string.Join("", lines.Take(lines.Count - 1));
                            currentMenu.MultilineDescription = string.Join("\r\n", lines.Take(lines.Count - 1));// TODO check if atleast 2 lines to begin with
                            currentMenu.FirstLine = lines.First(); // TODO check if atleast 2 lines to begin with
                            // TODO Detect pricing
                            step++;
                        }
                        else
                        {

                        }

                        break;
                    case 2:
                        if (child.Name == "img")
                        {
                            var altValue = child.GetAttributeValue("alt", "");

                            if (altValue == "vegan")
                                currentMenu.IsVegan = true;
                            else if (altValue == "mni_ubp_local")
                                currentMenu.IsLocal = true;
                            // TODO Detect pricing

                        }
                        else if (child.Name == "#text")
                        {
                            // #text is usually filler
                            // TODO If br hit twice go to next step
                        }
                        else if (child.Name == "br" && !brHitOnce)
                        {
                            // #text is usually filler
                            // TODO If br hit twice go to next step
                            brHitOnce = true;
                        }
                        else
                        {
                            // We no longer encounter images go to next step
                            step++;
                        }
                        break;
                    case 3:
                        if (child.Name == "table")
                        {
                            // Nutritions
                            step++;
                        }
                        else
                        {
                            // 

                        }
                        break;
                    case 4:
                        if (child.Name == "table")
                        {
                            // Balance
                            step++;
                        }
                        else
                        {
                            // 

                        }
                        break;
                    case 5:
                        if (child.Name == "p")
                        {
                            // Alergies

                            menus.Add(currentMenu);
                            currentMenu = new Menu();

                            step = 0; // end here
                        }
                        else
                        {
                            // 

                        }
                        break;

                    default:
                        break;
                }


            }


            foreach (var menu in menus)
            {
                //break;
                //await Context.Channel.SendMessageAsync($"**Menu: {menu.Description} Description: {menu.Name} IsVegan:{menu.IsVegan} IsLocal:{menu.IsLocal} Price: {menu.Price}**");

                //var reply = new GoogleEngine().ImageSearch(menu.FirstLine.Replace("\"", "").Trim(), lang: "de");



                //menu.ImgUrl = "https://cdn.discordapp.com/avatars/153929916977643521/5d6e05d48ab1b0599aa801ac4aebc1ea.png";
                menu.ImgUrl = GetImageFromGoole(menu.FirstLine, "de");

                if (menu.ImgUrl == "")
                    menu.ImgUrl = GetImageFromGoole(menu.Description, "de");

                if (menu.ImgUrl == "")
                    menu.ImgUrl = "https://cdn.discordapp.com/avatars/153929916977643521/5d6e05d48ab1b0599aa801ac4aebc1ea.png";




                //if (reply == "")
                //await Context.Channel.SendMessageAsync("Image not found");
                //else
                //await Context.Channel.SendMessageAsync(reply);






            }

            return menus;
        }


        private static string GetImageFromGoole(string text, string lang = "de")
        {
            return Google.ImageSearch(text.Replace("\"", "").Trim(), lang: lang);
        }

        public static Dictionary<Restaurant, List<Menu>> GetCurrentMenu(MealTime mealTime = MealTime.Lunch, Language language = Language.English, Location location = Location.Zentrum)
        {
            string day = "montag";

            switch (DateTime.Now.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    day = "montag";
                    break;
                case DayOfWeek.Tuesday:
                    day = "dienstag";
                    break;
                case DayOfWeek.Wednesday:
                    day = "mittwoch";
                    break;
                case DayOfWeek.Thursday:
                    day = "donnerstag";
                    break;
                case DayOfWeek.Friday:
                    day = "freitag";
                    break;
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                    day = "n/a";
                    break;
            }
            var lunchUzh = new List<Restaurant>()
            {
                Restaurant.UZH_UpperMensa,
                Restaurant.UZH_LowerMensa,
                //Restaurant.UZH_Lichthof_Rondel
            };

            if(mealTime == MealTime.Dinner)
            {
                lunchUzh = new List<Restaurant>() { Restaurant.UZH_LowerMensa_Dinner };
            }

            var menus = new Dictionary<Restaurant, List<Menu>>();

            try
            {
                menus.Add(Restaurant.ETH_Polymensa, GetPolymensaMenu(mealTime));
            }
            catch(Exception ex)
            {
            }
            
            foreach (var restaurant in lunchUzh)
            {
                try
                {
                    menus.Add(restaurant, GetUzhMenus(day, ToFriendlyString(restaurant)));
                }
                catch(Exception ex)
                {
                }
            }


            return menus;
        }

    }
}
