using Discord;
using Discord.Commands;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Data;
using ETHDINFKBot.Drawing;
using ETHDINFKBot.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ETHBot.DataLayer.Data.ETH.Food;

namespace ETHDINFKBot.Modules
{

    public class FoodModule : ModuleBase<SocketCommandContext>
    {
        // TODO DUPLICATE CODE
        private bool AllowedToRun(BotPermissionType type)
        {
            var channelSettings = DatabaseManager.Instance().GetChannelSetting(Context.Message.Channel.Id);
            if (Context.Message.Author.Id != Program.ApplicationSetting.Owner
                && !((BotPermissionType)(channelSettings?.ChannelPermissionFlags ?? 0)).HasFlag(type))
            {
#if DEBUG
                Context.Channel.SendMessageAsync("blocked by perms", false);
#endif
                return true;
            }

            return false;
        }

        // Temp solution to cache results
        static FoodDBManager FoodDBManager = FoodDBManager.Instance();
        private static Dictionary<string, SKBitmap> FoodImageCache = new Dictionary<string, SKBitmap>(); // TODO Clear this cache once a day

        private (SKBitmap Bitmap, int ImageId) GetFoodImage(Menu menu, int imgSize = 192)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    // TODO Fallback if the image cant be resolved
                    var menuImg = FoodDBManager.GetBestMenuImage(menu.MenuImageId ?? -1);
                    SKBitmap bitmap = null;

                    // Temp workaround to clear cache periodically as there wont be more than 100 images a day
                    // TODO Clear cache once a day at midnight
                    if (FoodImageCache.Count > 100)
                        FoodImageCache.Clear();

                    string imageUrl = menu.DirectMenuImageUrl;
                    if (string.IsNullOrEmpty(imageUrl))
                        imageUrl = menuImg?.MenuImageUrl ?? Program.Client.CurrentUser.GetAvatarUrl();

                    if (FoodImageCache.ContainsKey(imageUrl))
                    {
                        bitmap = FoodImageCache[imageUrl];
                    }
                    else
                    {
                        var bytes = webClient.DownloadData(imageUrl);
                        if (bytes == null)
                            return (null, -1);
                        bitmap = SKBitmap.Decode(bytes);
                        FoodImageCache.Add(imageUrl, bitmap);
                    }

                    if (bitmap != null)
                    {
                        int width = bitmap.Width;
                        int height = bitmap.Height;

                        if (width < height)
                        {
                            width = (int)(((decimal)imgSize / height) * width);
                            height = imgSize;
                        }
                        else
                        {
                            height = (int)(((decimal)imgSize / width) * height);
                            width = imgSize;
                        }

                        var resizedBitmap = bitmap.Resize(new SKSizeI(width, height), SKFilterQuality.High); //Resize to the canvas

                        return (resizedBitmap, menuImg?.MenuImageId ?? -1);
                    }
                    // TODO decide which image to return here
                    return (null, -1);
                }
            }
            catch (Exception ex)
            {
                return (null, -1);
            }
        }
        private (int UsedWidth, int UsedHeight) DrawMenu(SKCanvas canvas, Menu menu, int left, int top, int colWidth, MenuUserSetting menuUserSettings, int favRestaurantCount, bool debug)
        {
            var foodImage = GetFoodImage(menu);

            int usedHeight = 0;

            string menuName = menu.Name;

            if (debug)
                menuName += $" (Id: {menu.MenuId})";

            var titleFont = DrawingHelper.TitleTextPaint;
            titleFont.FakeBoldText = true;

            canvas.DrawText(menuName, new SKPoint(left, top), titleFont);
            usedHeight += 20;

            usedHeight += (int)DrawingHelper.DrawTextArea(
                canvas,
                DrawingHelper.MediumTextPaint,
                left,
                top + usedHeight,
                colWidth - 30,
                DrawingHelper.MediumTextPaint.TextSize,
                menu.Description
            );

            // Bring kcal a bit closer up
            usedHeight -= 5;

            // TODO for n/a values maybe hide it by default
            //canvas.DrawText(menu.Description, new SKPoint(, ), DrawingHelper.DefaultTextPaint);

            var kcalFont = DrawingHelper.TitleTextPaint;
            kcalFont.FakeBoldText = false;

            canvas.DrawText(menu.Calories > 0 ? menu.Calories + " kcal" : "n/a kcal", new SKPoint(left, usedHeight), kcalFont);
            usedHeight += 15;

            if (menuUserSettings?.FullNutritions == true)
            {
                canvas.DrawText(menu.Protein > 0 ? $"Protein: {menu.Protein} g" : "Protein: n/a", new SKPoint(left, usedHeight), DrawingHelper.MediumTextPaint);
                usedHeight += 14;
                canvas.DrawText(menu.Protein > 0 ? $"Fat: {menu.Fat} g" : "Fat: n/a", new SKPoint(left, usedHeight), DrawingHelper.MediumTextPaint);
                usedHeight += 14;
                canvas.DrawText(menu.Protein > 0 ? $"Carbohydrates: {menu.Carbohydrates} g" : "Carbohydrates: n/a", new SKPoint(left, usedHeight), DrawingHelper.MediumTextPaint);
                usedHeight += 14;
                canvas.DrawText(menu.Protein > 0 ? $"Salt: {menu.Salt} g" : "Salt: n/a", new SKPoint(left, usedHeight), DrawingHelper.MediumTextPaint);
                usedHeight += 14;
            }

            usedHeight += 5;
            canvas.DrawText("CHF " + menu.Amount.ToString("#,##0.00"), new SKPoint(left, usedHeight), titleFont);
            usedHeight += 15;



            if (menuUserSettings?.DisplayAllergies == true)
            {
                bool stringVersion = false;

                if (stringVersion)
                {
                    var AllergyString = FoodDBManager.GetMenuAllergiesString(menu);
                    if (!string.IsNullOrWhiteSpace(AllergyString))
                    {
                        usedHeight += 5;
                        //canvas.DrawText(, new SKPoint(left, usedHeight), DrawingHelper.MediumTextPaint);
                        // TODO Replace Allergy text with images
                        usedHeight = (int)DrawingHelper.DrawTextArea(
                            canvas,
                            DrawingHelper.MediumTextPaint,
                            left,
                            usedHeight,
                            colWidth - 30,
                            DrawingHelper.MediumTextPaint.TextSize,
                            "Allergies: " + AllergyString
                        );

                        usedHeight += 5;
                    }
                }
                else
                {
                    // Icons version

                    var pathToAllergyImages = Path.Combine(Program.ApplicationSetting.BasePath, "Images", "Icons", "Food", "Allergies");

                    var allergyIds = FoodDBManager.GetMenuAllergyIds(menu);

                    int allergyIconOffset = 0;
                    foreach (var allergyId in allergyIds)
                    {
                        var allergyBitmap = SKBitmap.Decode(Path.Combine(pathToAllergyImages, $"{allergyId:D2}.png"));
                        if (allergyBitmap == null)
                        {
                            continue; //TODO This shouldnt happen -> LOG
                        }

                        allergyBitmap = allergyBitmap.Resize(new SKSizeI(32, 32), SKFilterQuality.High);
                        canvas.DrawBitmap(allergyBitmap, new SKPoint(left + allergyIconOffset * 36, usedHeight));
                        allergyIconOffset++;
                    }

                    if (allergyIconOffset > 0)
                        usedHeight += 40;
                }
            }

            // Insert if is vegan or vegetarian
            // TODO Load those bitmaps in an aux method
            var pathToImage = Path.Combine(Program.ApplicationSetting.BasePath, "Images", "Icons", "Food");

            // TODO Rework icon code
            int iconOffset = 0;
            if (menu.IsVegetarian ?? false)
            {
                var vegetarianBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "vegetarian.png"));
                canvas.DrawBitmap(vegetarianBitmap, new SKPoint(left + iconOffset * 40, usedHeight));
                iconOffset++;
            }

            if (menu.IsVegan ?? false)
            {
                var veganBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "vegan.png"));
                canvas.DrawBitmap(veganBitmap, new SKPoint(left + iconOffset * 40, usedHeight));
                iconOffset++;
            }

            if (menu.IsLocal ?? false)
            {
                var veganBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "local.png"));
                canvas.DrawBitmap(veganBitmap, new SKPoint(left + iconOffset * 40, usedHeight));
                iconOffset++;
            }

            if (menu.IsBalanced ?? false)
            {
                var veganBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "balanced.png"));
                canvas.DrawBitmap(veganBitmap, new SKPoint(left + iconOffset * 40, usedHeight));
                iconOffset++;
            }

            if (menuUserSettings == null && favRestaurantCount == 0)
            {
                // User hasnt favourited any menu preference or locations 
                // TODO draw hint on image?
            }

            // TODO Icons for these 2 or are these even provided
            /*
            if (menu.IsGlutenFree ?? false)
            {
                var veganBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "vegan.png"));
                canvas.DrawBitmap(veganBitmap, new SKPoint(left + iconOffset * 40, usedHeight));
                usedHeight += 40;
                iconOffset++;
            }

            if (menu.IsLactoseFree ?? false)
            {
                var veganBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "vegan.png"));
                canvas.DrawBitmap(veganBitmap, new SKPoint(left + iconOffset * 40, usedHeight));
                usedHeight += 40;
                iconOffset++;
            }*/

            if (iconOffset > 0)
                usedHeight += 40;

            if (foodImage.Bitmap != null)
            {
                canvas.DrawBitmap(foodImage.Bitmap, new SKPoint(left, usedHeight));

                if (!string.IsNullOrWhiteSpace(menu.DirectMenuImageUrl))
                {
                    var verifiedBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "verified.png"));
                    canvas.DrawBitmap(verifiedBitmap, new SKPoint(left, usedHeight));
                }

                if (debug)
                {
                    SKColor shadowColor = new SKColor(0, 0, 0, 128);

                    var debugImageIdFont = DrawingHelper.LargeTextPaint;
                    debugImageIdFont.FakeBoldText = true;
                    debugImageIdFont.BlendMode = SKBlendMode.Overlay;
                    debugImageIdFont.BlendMode = SKBlendMode.Xor;
                    debugImageIdFont.BlendMode = SKBlendMode.Screen;

                    //debugImageIdFont.BlendMode = SKBlendMode.SoftLight;
                    //debugImageIdFont.BlendMode = SKBlendMode.SrcIn;
                    //debugImageIdFont.IsLinearText = true;

                    SKPaint shadowPaint = new SKPaint()
                    {
                        Color = shadowColor,
                        //BlendMode = SKBlendMode.SoftLight
                    };

                    canvas.DrawRect(left, usedHeight, foodImage.Bitmap.Width, 40, shadowPaint);

                    //debugImageIdFont.ImageFilter = SKImageFilter.CreateDropShadow(2, 2, 4, 4, shadowColor);
                    canvas.DrawText("ImageId: " + foodImage.ImageId, new SKPoint(left + 7, usedHeight + 27), debugImageIdFont);
                }

                usedHeight += foodImage.Bitmap.Height + 20; // Add 20 to bottom padding
            }

            return (colWidth, usedHeight);
        }

        private Dictionary<Restaurant, List<Menu>> GetDefaultMenuList(MealTime mealtime, MenuUserSetting userSettings)
        {
            var currentMenus = new Dictionary<Restaurant, List<Menu>>();

            var defaultLunchRestaurants = new List<Restaurant>()
            {
                FoodDBManager.GetRestaurantByName("ETH Polymensa (Lunch)"),
                FoodDBManager.GetRestaurantByName("UZH Zentrum Lower Mensa (Lunch)"),
                FoodDBManager.GetRestaurantByName("UZH Zentrum Upper Mensa (Lunch)")
            };

            var defaultDinnerRestaurants = new List<Restaurant>()
            {
                FoodDBManager.GetRestaurantByName("ETH Polymensa (Dinner)"),
                FoodDBManager.GetRestaurantByName("UZH Zentrum Lower Mensa (Dinner)")
            };

            var defaultRestaurant = defaultLunchRestaurants;
            if (mealtime == MealTime.Lunch)
                defaultRestaurant = defaultLunchRestaurants;
            else if (mealtime == MealTime.Dinner)
                defaultRestaurant = defaultDinnerRestaurants;

            foreach (var restaurant in defaultRestaurant)
            {
                var defaultMenu = FoodDBManager.GetMenusFromRestaurant(restaurant.RestaurantId, DateTime.Now);

                if (userSettings?.VeganPreference == true)
                    defaultMenu = defaultMenu.Where(i => i.IsVegan ?? false).ToList();
                if (userSettings?.VegetarianPreference == true)
                    defaultMenu = defaultMenu.Where(i => (i.IsVegetarian ?? false) || (i.IsVegan ?? false)).ToList();

                if (defaultMenu.Count == 0)
                    continue;

                currentMenus.Add(restaurant, defaultMenu);
            }

            return currentMenus;
        }

        [Command("food")]
        [Priority(1)]
        public async Task DrawFoodImages(string day = null, string time = null, bool debug = false)
        {
            try
            {
                if (AllowedToRun(BotPermissionType.EnableType2Commands))
                    return;

                await Context.Message.Channel.SendMessageAsync("Command entered");

                var meal = MealTime.Lunch;

                var searchDate = DateTime.UtcNow.UtcToLocalDateTime(Program.TimeZoneInfo); /// Make it passable by param
                if (searchDate.Hour < 6)
                {
                    await Context.Message.Channel.SendMessageAsync("You are here too early. The menus will be loading during the night. Come back in the morning. Good night <:sleep:851469700453367838>", messageReference: new MessageReference(Context.Message.Id));
                    return;
                }

                if (searchDate.Hour >= 14)
                    meal = MealTime.Dinner;

                if (day == null)
                    day = "";

                if (time == null)
                    time = day; // in this case day is actually time

                if (time.ToLower() == "lunch")
                    meal = MealTime.Lunch;
                else if (time.ToLower() == "dinner")
                    meal = MealTime.Dinner;

                var author = Context.Message.Author;
                var userId = author.Id;

                DayOfWeek dayOfWeek = DateTime.UtcNow.DayOfWeek;

                if (day.ToLower() == "monday" || day.ToLower() == "mon")
                    dayOfWeek = DayOfWeek.Monday;
                else if (day.ToLower() == "tuesday" || day.ToLower() == "tue")
                    dayOfWeek = DayOfWeek.Tuesday;
                else if (day.ToLower() == "wednesday" || day.ToLower() == "wed")
                    dayOfWeek = DayOfWeek.Wednesday;
                else if (day.ToLower() == "thursday" || day.ToLower() == "thu")
                    dayOfWeek = DayOfWeek.Thursday;
                else if (day.ToLower() == "friday" || day.ToLower() == "fri")
                    dayOfWeek = DayOfWeek.Friday;
                else if (day.ToLower() == "saturday" || day.ToLower() == "sat")
                    dayOfWeek = DayOfWeek.Saturday;
                else if (day.ToLower() == "sunday" || day.ToLower() == "sun")
                    dayOfWeek = DayOfWeek.Sunday;

                // get this weeks monday
                var date = DateTime.UtcNow;
                date = date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);

                // remove time
                date = date.Date;

                while (date.DayOfWeek != dayOfWeek)
                    date = date.AddDays(1);

                searchDate = date;

                //// Only allow bot owner go into debug mode
                // if (userId != Program.ApplicationSetting.Owner)
                //    debug = false;

                var userFavRestaurants = FoodDBManager.GetUsersFavouriteRestaurants(userId);
                var userSettings = FoodDBManager.GetUserFoodSettings(userId);


                var currentMenus = new Dictionary<Restaurant, List<Menu>>();

                // TODO Dinner options
                if (userFavRestaurants.Count == 0)
                {
                    currentMenus = GetDefaultMenuList(meal, userSettings);
                }
                else
                {
                    foreach (var favRestaurant in userFavRestaurants)
                    {
                        var restaurant = FoodDBManager.GetRestaurantById(favRestaurant.RestaurantId);

                        if (meal == MealTime.Lunch && !restaurant.OffersLunch)
                            continue;// Request needs lunch

                        if (meal == MealTime.Dinner && !restaurant.OffersDinner)
                            continue; // Request needs dinner

                        var menus = FoodDBManager.GetMenusFromRestaurant(favRestaurant.RestaurantId, searchDate);

                        // TODO Duplicate code
                        if (userSettings?.VeganPreference == true)
                            menus = menus.Where(i => i.IsVegan ?? false).ToList();
                        if (userSettings?.VegetarianPreference == true)
                            menus = menus.Where(i => (i.IsVegetarian ?? false) || (i.IsVegan ?? false)).ToList();

                        if (menus.Count == 0)
                            continue;

                        currentMenus.Add(FoodDBManager.GetRestaurantById(favRestaurant.RestaurantId), menus);
                    }
                }

                if (currentMenus.Count == 0)
                {
                    string weekendString = "";
                    if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                        weekendString = Environment.NewLine + "Also remember its weekend :D";

                    if (userFavRestaurants.Count == 0)
                        await Context.Message.Channel.SendMessageAsync("No menus found, likely there are none available today :(" + weekendString, messageReference: new MessageReference(Context.Message.Id));
                    else
                        await Context.Message.Channel.SendMessageAsync(@$"No menus found, likely that you haven't favourited any restaurants for the current meal time: **{meal}**
Type **{Program.CurrentPrefix}food fav** to see your current food settings and to also change them.
It is also likely that there are no menus currently available today." + weekendString, messageReference: new MessageReference(Context.Message.Id));

                    return;
                }


                var padding = DrawingHelper.DefaultPadding;

                padding.Left = 20;
                padding.Top = 40;

                int imgSize = 192;

                int rowHeight = 500; // cut off in the end
                int colWidth = 50 + imgSize;

                var paint = DrawingHelper.DefaultTextPaint;
                paint.TextSize = 20;
                paint.Color = new SKColor(128, 255, 64);

                List<Stream> streams = new List<Stream>();

                var pathToImage = Path.Combine(Program.ApplicationSetting.BasePath, "Images", "Icons", "Food");

                int maxMenus = currentMenus.Count == 0 ? 0 : currentMenus.Values.Max(i => i.Count);

                var largeFont = DrawingHelper.LargeTextPaint;
                largeFont.FakeBoldText = true;

                bool mergeMode = userSettings?.VegetarianPreference == true
                                || userSettings?.VeganPreference == true
                                || currentMenus.Count() > 5;

                if (mergeMode)
                {
                    int maxPages = 4;

                    int totalMenus = currentMenus.Count == 0 ? 0 : currentMenus.Sum(i => i.Value.Count);
                    int maxColumns = Math.Max(3, totalMenus / maxPages);

                    // TODO Title max chars 
                    var (canvas, bitmap) = DrawingHelper.GetEmptyGraphics(2_000, 2_000);
                    int currentColumn = 0;

                    int maxUsedHeight = 0;
                    int currentWidth = 0;
                    int currentTitleTop = 0;
                    try
                    {
                        foreach (var restaurant in currentMenus)
                        {
                            foreach (var menu in restaurant.Value)
                            {
                                if (currentColumn == 0)
                                    (canvas, bitmap) = DrawingHelper.GetEmptyGraphics(2_000, 2_000);

                                int currentTop = 0;
                                string restaurantName = restaurant.Key.Name;

                                // add date and day of week for restaurant
                                restaurantName += $" ({searchDate.ToString("dd.MM.yyyy")}) - {searchDate.DayOfWeek}";

                                if (debug)
                                    restaurantName += $" (Id: {restaurant.Key.RestaurantId})";


                                // Display restaurant name if new row started or if new restaurant
                                if (currentColumn == 0 || restaurant.Value.IndexOf(menu) == 0)
                                {
                                    currentTop = (int)DrawingHelper.DrawTextArea(canvas, largeFont, padding.Left + currentColumn * colWidth, padding.Top + currentTop, colWidth - 30, largeFont.TextSize, restaurantName);
                                    currentTop -= 35;

                                    if (currentTitleTop < currentTop)
                                        currentTitleTop = currentTop;
                                    else
                                        currentTop = currentTitleTop;
                                }
                                else
                                {
                                    currentTop = currentTitleTop;
                                }

                                //canvas.DrawText(meal.ToString(), new SKPoint(1/*TODO which value here maybe in the end*/ * colWidth - 75, 35), paint);


                                (int usedWidth, int usedHeight) = DrawMenu(canvas, menu, padding.Left + currentColumn * colWidth, padding.Top + currentTop, colWidth, userSettings, userFavRestaurants.Count, debug);

                                currentWidth += usedWidth;

                                if (maxUsedHeight < usedHeight)
                                    maxUsedHeight = usedHeight;


                                currentColumn++;
                                if (currentColumn >= maxColumns)
                                {
                                    // TODO currentWidth sometimes has wrong values
                                    bitmap = DrawingHelper.CropImage(bitmap, new SKRect(0, 0, currentWidth, maxUsedHeight));

                                    var stream = CommonHelper.GetStream(bitmap);
                                    if (stream != null)
                                        streams.Add(stream);

                                    bitmap.Dispose();
                                    canvas.Dispose();

                                    currentColumn = 0;
                                    maxUsedHeight = 0;
                                    currentWidth = 0;
                                    currentTitleTop = 0;
                                }
                            }
                        }

                        if (currentColumn > 0)
                        {
                            bitmap = DrawingHelper.CropImage(bitmap, new SKRect(0, 0, currentWidth, maxUsedHeight));
                            // TODO Crop last one and change this logic here
                            var stream = CommonHelper.GetStream(bitmap);
                            if (stream != null)
                                streams.Add(stream);


                            bitmap.Dispose();
                            canvas.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                    foreach (var restaurant in currentMenus)
                    {
                        int maxUsedHeight = 0;
                        int maxUsedWidth = 0;

                        if (restaurant.Value.Count == 0)
                            continue;

                        // Set max menus for now per restaurant
                        maxMenus = restaurant.Value.Count;

                        // TODO Ensure the width size isnt violated 
                        var (canvas, bitmap) = DrawingHelper.GetEmptyGraphics(2_000, 2_000);

                        int currentTop = 0;
                        string restaurantName = restaurant.Key.Name;

                        // add date and day of week for restaurant
                        restaurantName += $" ({searchDate.ToString("dd.MM.yyyy")}) - {searchDate.DayOfWeek}";

                        if (debug)
                            restaurantName += $" (Id: {restaurant.Key.RestaurantId})";

                        canvas.DrawText(restaurantName, new SKPoint(padding.Left, padding.Top + currentTop), largeFont); // TODO Correct paint?

                        currentTop += 25;

                        int column = 0;
                        int row = 0;

                        int currentWidth = 0;

                        int maxColumnCount = Math.Min(3, maxMenus);

                        // Limit to 2 rows max
                        if (maxMenus > 3)
                            maxColumnCount = (int)Math.Ceiling(maxMenus / 2m);


                        canvas.DrawText(meal.ToString(), new SKPoint(maxColumnCount * colWidth - 75, 35), paint);

                        foreach (var menu in restaurant.Value)
                        {
                            (int usedWidth, int usedHeight) = DrawMenu(canvas, menu, padding.Left + column * colWidth, padding.Top + currentTop, colWidth, userSettings, userFavRestaurants.Count, debug);

                            currentWidth += usedWidth;

                            if (maxUsedHeight < usedHeight)
                                maxUsedHeight = usedHeight;

                            if (maxUsedWidth < currentWidth)
                                maxUsedWidth = currentWidth;

                            column++;

                            if (column >= maxColumnCount)
                            {
                                row++;
                                column = 0;
                                currentTop = maxUsedHeight - 20;
                                ///maxUsedHeight = 0; TODO Reset per row
                                currentWidth = 0;
                            }
                        }

                        //maxUsedHeight += 20; // Bottom padding

                        /* await Context.Channel.SendMessageAsync($"**Menu: {menu.Name} Description: {menu.Description} Price: {menu.Price}**");

                         if (!string.IsNullOrEmpty(menu.ImgUrl))
                             await Context.Channel.SendMessageAsync(menu.ImgUrl);
                        */
                        bitmap = DrawingHelper.CropImage(bitmap, new SKRect(0, 0, maxUsedWidth, maxUsedHeight));


                        var stream = CommonHelper.GetStream(bitmap);
                        if (stream != null)
                            streams.Add(stream);

                        if (streams.Count >= 5)
                            break; // Limit to 5 max

                        bitmap.Dispose();
                        canvas.Dispose();

                    }
                }

                paint = DrawingHelper.DefaultTextPaint;
                paint.TextSize = 20;
                paint.Color = new SKColor(255, 32, 32);
                //canvas.DrawText("THIS FEATURE IS IN ALPHA CURRENTLY", new SKPoint(padding.Left, bitmap.Height - 50), paint);
                paint.TextSize = 16;
                //canvas.DrawText("(Images are taken from google.com and may not represent the actual product)", new SKPoint(padding.Left, bitmap.Height - 30), paint);


                var attachments = new List<FileAttachment>();
                // TODO send multiple attachments
                int menuCount = 0;
                foreach (var stream in streams)
                    attachments.Add(new FileAttachment(stream, $"menu_{menuCount}.png"));

                if (attachments.Count > 0)
                    await Context.Channel.SendFilesAsync(attachments, messageReference: new MessageReference(Context.Message.Id));

                if (userFavRestaurants.Count == 0)
                {
                    // User hasnt favourited any menu preference or locations 
                    // Always show this hint
                    await Context.Channel.SendMessageAsync($"You haven't set any favourite mensa location. The bot will show you a default view only (Polymensa and UZH Zentrum Mensa).{Environment.NewLine}" +
                        $"You can adjust this with {Program.CurrentPrefix}food fav", messageReference: new MessageReference(Context.Message.Id));
                }

                //    // Create the service.
                //    var service = new CustomSearchAPIService(new BaseClientService.Initializer
                //    {
                //        //ApplicationName = "Discovery Sample",
                //        ApiKey = "",
                //    });

                //    // Run the request.
                //    Console.WriteLine("Executing a list request...");
                //    CseResource.ListRequest listRequest = new CseResource.ListRequest(service)
                //    {
                //        Cx = "",
                //        Q = polymensaMenus[0].FirstLine,
                //        Safe = CseResource.ListRequest.SafeEnum.Active,
                //        SearchType = CseResource.ListRequest.SearchTypeEnum.Image,
                //        Hl = "de"
                //    };


                //    try
                //    {

                //        Search search = listRequest.Execute();
                //        // Display the results.
                //        if (search.Items != null)
                //        {
                //            foreach (var api in search.Items)
                //            {
                //                Context.Channel.SendMessageAsync(api.Link);
                //                Console.WriteLine(api.DisplayLink + " - " + api.Title);
                //            }
                //        }
                //    }
                //    catch (GoogleApiException e)
                //    {
                //        Console.WriteLine($"statuscode:{e.HttpStatusCode}");
                //    }

            }
            catch (Exception ex)
            {
                // TODO check if the error code can be extracted from the exception directly
                if (ex.Message.Contains("50035"))
                {
                    await Context.Channel.SendMessageAsync($"Think ya funny <@{Context.Message.Author.Id}>?");
                }
                else
                {
                    await Context.Channel.SendMessageAsync(ex.ToString());
                }
            }
        }


        [Group("food")]
        public class RantAdminModule : ModuleBase<SocketCommandContext>
        {
            private static FoodDBManager FoodDBManager = FoodDBManager.Instance();
            [Command("help")]
            [Priority(10)]
            public async Task FoodHelp()
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Food Help");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField($"{Program.CurrentPrefix}food help", "This message :)");
                builder.AddField($"{Program.CurrentPrefix}food <lunch|dinner>", $"Retreived food info. If the user has no settings then default mensas are retreived.{Environment.NewLine}" +
                    $"Optional: Time parameter 'lunch' or 'dinner'. If its not provided then the bot send currently appropriate menus depending on the time of day.");
                builder.AddField($"{Program.CurrentPrefix}food fav {Program.CurrentPrefix}food settings", $"Edit your favourite restaurants and food preferences which are used when the user calls {Program.CurrentPrefix}food");
                builder.AddField($"{Program.CurrentPrefix}food allergies", "Informations about the Allergy icons");
                builder.AddField($"{Program.CurrentPrefix}admin food help", "For admins");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("allergies")]
            [Priority(10)]
            public async Task FoodAllergyInfo()
            {
                var pathToAllergyImages = Path.Combine(Program.ApplicationSetting.BasePath, "Images", "Icons", "Food", "Allergies");

                var (canvas, bitmap) = DrawingHelper.GetEmptyGraphics(650, 650);


                var Allergies = FoodDBManager.GetAllergies();

                int usedHeight = 20; // padding
                int left = 20;

                var largeFont = DrawingHelper.LargeTextPaint;
                largeFont.FakeBoldText = true;

                foreach (var Allergy in Allergies)
                {
                    var AllergyBitmap = SKBitmap.Decode(Path.Combine(pathToAllergyImages, $"{Allergy.AllergyId:D2}.png"));
                    if (AllergyBitmap == null)
                    {
                        continue; //TODO This shouldnt happen -> LOG
                    }

                    AllergyBitmap = AllergyBitmap.Resize(new SKSizeI(40, 40), SKFilterQuality.High);
                    canvas.DrawBitmap(AllergyBitmap, new SKPoint(left, usedHeight));


                    canvas.DrawText($"{Allergy.Name} / {Allergy.NameDE}", new SKPoint(left + 50, usedHeight + 28), largeFont);

                    usedHeight += 44;
                }

                var stream = CommonHelper.GetStream(bitmap);

                await Context.Message.Channel.SendFileAsync(stream, "Allergies.png");
            }

            [Command("fav")]
            [Alias("settings")]
            [Priority(10)]
            public async Task FoodSettings()
            {

                var currentUser = Context.Message.Author;
                var currentUserId = currentUser.Id;


                var userMenuSetting = FoodDBManager.GetUserFoodSettings(currentUserId);
                var userFavRestaurants = FoodDBManager.GetUsersFavouriteRestaurants(currentUserId);
                var availableRestaurants = FoodDBManager.GetAllRestaurants();


                if (userMenuSetting == null)
                    userMenuSetting = new MenuUserSetting();

                // Get Current settings
                // Get Current faved restaurants

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Food settings for {currentUser.Username}"); // TODO Nickname

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(currentUser.GetAvatarUrl() ?? currentUser.GetDefaultAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.WithDescription(@"You can click on the buttons bellow to adjust your settings. Blue buttons mean active setting, red meaning inactive.");
                builder.WithAuthor(currentUser);
                //builder.AddField("test", "test");

                var builderComponent = new ComponentBuilder();

                try
                {
                    builderComponent.WithButton("Filter Vegetarian", $"food-fav-vegetarian", userMenuSetting.VegetarianPreference ? ButtonStyle.Primary : ButtonStyle.Danger, Emote.Parse($"<:food_vegetarian:1017751739648188487>"), null, false, 0);
                    builderComponent.WithButton("Filter Vegan", $"food-fav-vegan", userMenuSetting.VeganPreference ? ButtonStyle.Primary : ButtonStyle.Danger, Emote.Parse($"<:food_vegan:1017751741455937536>"), null, false, 0);
                    builderComponent.WithButton("Show all nutritions stats", $"food-fav-nutritions", userMenuSetting.FullNutritions ? ButtonStyle.Primary : ButtonStyle.Danger, null/*Emote.Parse($"<:food_vegan:1017751741455937536>")*/, null, false, 0);
                    builderComponent.WithButton("Show Allergies", $"food-fav-allergies", userMenuSetting.DisplayAllergies ? ButtonStyle.Primary : ButtonStyle.Danger, null/*Emote.Parse($"<:food_vegan:1017751741455937536>")*/, null, false, 0);

                    var favedRestaurantIds = userFavRestaurants.Select(i => i.RestaurantId);

                    int row = 1;
                    foreach (var restaurantLocationGroup in availableRestaurants.GroupBy(i => i.Location))
                    {
                        // Currently only supports only 4 locations with 5 restaurants each

                        foreach (var restaurant in restaurantLocationGroup)
                        {
                            builderComponent.WithButton(restaurant.Name, $"food-fav-{restaurant.RestaurantId}", favedRestaurantIds.Contains(restaurant.RestaurantId) ? ButtonStyle.Primary : ButtonStyle.Danger, null, null, false, row);
                        }

                        row++;
                    }
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.ToString());
                }

                await Context.Channel.SendMessageAsync("", false, builder.Build(), components: builderComponent.Build());
            }
        }
    }
}
