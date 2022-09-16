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
                && !((BotPermissionType)channelSettings?.ChannelPermissionFlags).HasFlag(type))
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
        private SKBitmap GetFoodImage(Menu menu, int imgSize = 192)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    var menuImg = FoodDBManager.GetBestMenuImage(menu.MenuImageId ?? -1);
                    var imgBytes = webClient.DownloadData(menuImg?.MenuImageUrl ?? Program.Client.CurrentUser.GetAvatarUrl());
                    if (imgBytes == null)
                        return null;

                    var bitmap = SKBitmap.Decode(imgBytes);
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

                        return resizedBitmap;
                    }
                    // TODO decide which image to return here
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private (int UsedWidth, int UsedHeight) DrawMenu(SKCanvas canvas, Menu menu, int left, int top, int colWidth)
        {
            var foodImage = GetFoodImage(menu);

            int usedHeight = 0;

            canvas.DrawText(menu.Name, new SKPoint(left, top), DrawingHelper.TitleTextPaint);
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

            // TODO for n/a values maybe hide it by default
            //canvas.DrawText(menu.Description, new SKPoint(, ), DrawingHelper.DefaultTextPaint);
            canvas.DrawText(menu.Calories > 0 ? menu.Calories + " kcal" : "n/a kcal", new SKPoint(left, usedHeight), DrawingHelper.TitleTextPaint);
            usedHeight += 15;

            bool showFullNutrutions = true; // TODO Setting for users
            if (showFullNutrutions)
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
            canvas.DrawText("CHF " + menu.Amount.ToString("#,##0.00"), new SKPoint(left, usedHeight), DrawingHelper.TitleTextPaint);
            usedHeight += 10;

            // Insert if is vegan or vegetarian
            // TODO Load those bitmaps in an aux method
            var pathToImage = Path.Combine("Images", "Icons", "Food");
            if (menu.IsVegetarian ?? false)
            {
                var vegetarianBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "vegetarian.png"));
                canvas.DrawBitmap(vegetarianBitmap, new SKPoint(left, usedHeight));
                usedHeight += 40;
            }

            if (menu.IsVegan ?? false)
            {
                var veganBitmap = SKBitmap.Decode(Path.Combine(pathToImage, "vegan.png"));
                canvas.DrawBitmap(veganBitmap, new SKPoint(left, usedHeight));
                usedHeight += 40;
            }

            if (foodImage != null)
            {
                canvas.DrawBitmap(foodImage, new SKPoint(left, usedHeight));
                usedHeight += foodImage.Height + 20; // Add 20 to bottom padding
            }

            return (colWidth, usedHeight);
        }

        private Dictionary<ETHBot.DataLayer.Data.ETH.Food.Restaurant, List<Menu>> GetDefaultMenuList(MealTime mealtime, MenuUserSetting userSettings)
        {
            var currentMenus = new Dictionary<ETHBot.DataLayer.Data.ETH.Food.Restaurant, List<Menu>>();

            var defaultLunchRestaurants = new List<ETHBot.DataLayer.Data.ETH.Food.Restaurant>()
            {
                FoodDBManager.GetRestaurantByName("ETH Polymensa (Lunch)"),
                FoodDBManager.GetRestaurantByName("UZH Zentrum Lower Mensa (Lunch)")
            };

            var defaultDinnerRestaurants = new List<ETHBot.DataLayer.Data.ETH.Food.Restaurant>()
            {
                FoodDBManager.GetRestaurantByName("ETH Polymensa (Dinner)"),
                FoodDBManager.GetRestaurantByName("UZH Zentrum Lower Mensa (Dinner)")
            };

            var defaultRestaurant = defaultLunchRestaurants;
            if (mealtime == MealTime.Lunch)
                defaultRestaurant = defaultLunchRestaurants;
            else if (mealtime == MealTime.Dinner)
                defaultRestaurant = defaultDinnerRestaurants;

            foreach (var restaurant in defaultLunchRestaurants)
            {
                var defaultMenu = FoodDBManager.GetMenusFromRestaurant(restaurant.RestaurantId, DateTime.Now);

                if (userSettings?.VeganPreference == true)
                    defaultMenu = defaultMenu.Where(i => i.IsVegan ?? false).ToList();
                if (userSettings?.VegetarianPreference == true)
                    defaultMenu = defaultMenu.Where(i => (i.IsVegetarian ?? false) || (i.IsVegan ?? false)).ToList();

                if (defaultMenu.Count == 0)
                    continue;
            }

            return currentMenus;
        }

        [Command("food")]
        public async Task FoodB(bool refresh = false)
        {
            try
            {
                if (AllowedToRun(BotPermissionType.EnableType2Commands))
                    return;

                var meal = MealTime.Lunch;

                var searchDate = DateTime.Now;//.AddDays(-1);

                // TODO Do CET/CEST Switch
                //if (DateTime.UtcNow.Hour >= 12)
                //    meal = MealTime.Dinner;

                // Only allow bot owner to reload cache
                var author = Context.Message.Author;
                if (refresh && author.Id != Program.ApplicationSetting.Owner)
                    refresh = false;


                var userId = author.Id;

                var userFavRestaurants = FoodDBManager.GetUsersFavouriteRestaurants(userId);
                var userSettings = FoodDBManager.GetUserFoodSettings(userId);


                var currentMenus = new Dictionary<ETHBot.DataLayer.Data.ETH.Food.Restaurant, List<Menu>>();

                // TODO Dinner options
                if (userFavRestaurants.Count == 0)
                {
                    currentMenus = GetDefaultMenuList(MealTime.Lunch, userSettings);
                }
                else
                {
                    foreach (var favRestaurant in userFavRestaurants)
                    {
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

                var pathToImage = Path.Combine("Images", "Icons", "Food");

                int maxMenus = currentMenus.Values.Max(i => i.Count);


                foreach (var restaurant in currentMenus)
                {
                    int maxUsedHeight = 0;
                    int maxUsedWidth = 0;

                    if (restaurant.Value.Count == 0)
                        continue;

                    // Set max menus for now per restaurant
                    maxMenus = restaurant.Value.Count;

                    var (canvas, bitmap) = DrawingHelper.GetEmptyGraphics(1_000, 1_000);
                    canvas.DrawText(meal.ToString(), new SKPoint(maxMenus * colWidth - 100, 25), paint);


                    int currentTop = 0;

                    canvas.DrawText(restaurant.Key.Name, new SKPoint(padding.Left, padding.Top + currentTop), DrawingHelper.LargeTextPaint); // TODO Correct paint?

                    currentTop += 25;

                    int column = 0;
                    int row = 0;

                    int currentWidth = 0;

                    int maxColumnCount = 3;

                    // Limit to 2 rows max
                    if (maxMenus > 3)
                        maxColumnCount = (int)Math.Ceiling(maxMenus / 2m);

                    foreach (var menu in restaurant.Value)
                    {
                        (int usedWidth, int usedHeight) = DrawMenu(canvas, menu, padding.Left + column * colWidth, padding.Top + currentTop, colWidth);

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
                            currentTop = maxUsedHeight;
                            ///maxUsedHeight = 0; TODO Reset per row
                            currentWidth = 0;
                        }
                    }



                    /* await Context.Channel.SendMessageAsync($"**Menu: {menu.Name} Description: {menu.Description} Price: {menu.Price}**");

                     if (!string.IsNullOrEmpty(menu.ImgUrl))
                         await Context.Channel.SendMessageAsync(menu.ImgUrl);
                    */
                    bitmap = DrawingHelper.CropImage(bitmap, new SKRect(0, 0, maxUsedWidth, maxUsedHeight));


                    var stream = CommonHelper.GetStream(bitmap);
                    if (stream != null)
                        streams.Add(stream);

                    bitmap.Dispose();
                    canvas.Dispose();

                }

                paint = DrawingHelper.DefaultTextPaint;
                paint.TextSize = 20;
                paint.Color = new SKColor(255, 32, 32);
                //canvas.DrawText("THIS FEATURE IS IN ALPHA CURRENTLY", new SKPoint(padding.Left, bitmap.Height - 50), paint);
                paint.TextSize = 16;
                //canvas.DrawText("(Images are taken from google.com and may not represent the actual product)", new SKPoint(padding.Left, bitmap.Height - 30), paint);



                // TODO send multiple attachments
                foreach (var stream in streams)
                    await Context.Channel.SendFileAsync(stream, "menu.png", $"");



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
                await Context.Channel.SendMessageAsync(ex.ToString());

            }
        }


        [Group("food")]
        public class RantAdminModule : ModuleBase<SocketCommandContext>
        {
            private static FoodDBManager FoodDBManager = FoodDBManager.Instance();
            [Command("fav")]
            public async Task FoodSettings()
            {
                var currentUserId = Context.Message.Author.Id;


                var userMenuSetting = FoodDBManager.GetUserFoodSettings(currentUserId);
                var userFavRestaurants = FoodDBManager.GetUsersFavouriteRestaurants(currentUserId);
                var availableRestaurants = FoodDBManager.GetAllRestaurants();


                if (userMenuSetting == null)
                    userMenuSetting = new ETHBot.DataLayer.Data.ETH.Food.MenuUserSetting();

                // Get Current settings
                // Get Current faved restaurants

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Food settings");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("test", "test");

                var builderComponent = new ComponentBuilder();

                try
                {
                    builderComponent.WithButton("Filter Vegetarian", $"food-fav-vegetarian", userMenuSetting.VegetarianPreference ? ButtonStyle.Primary : ButtonStyle.Danger, Emote.Parse($"<:food_vegetarian:1017751739648188487>"), null, false, 0);
                    builderComponent.WithButton("Filter Vegan", $"food-fav-vegan", userMenuSetting.VeganPreference ? ButtonStyle.Primary : ButtonStyle.Danger, Emote.Parse($"<:food_vegan:1017751741455937536>"), null, false, 0);

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

                }


                await Context.Channel.SendMessageAsync("", false, builder.Build(), components: builderComponent.Build());
            }

/*
            [Command("load")]
            public async Task LoadMenus()
            {
                var avilableRestaurants = FoodDBManager.GetAllRestaurants();

                foreach (var restaurant in avilableRestaurants)
                {
                    if (restaurant.IsOpen)
                    {
                        await Context.Channel.SendMessageAsync($"Processing {restaurant.Name}");

                        List<Menu> menu = new List<Menu>();
                        switch (restaurant.Location)
                        {
                            case RestaurantLocation.None:
                                break;

                            // SV Restaurant handler
                            case RestaurantLocation.ETH_Zentrum:
                            case RestaurantLocation.ETH_Hoengg:

                                var svRestaurantMenuInfos = FoodHelper.GetSVRestaurantMenu(restaurant.MenuUrl);
                                await Context.Channel.SendMessageAsync($"Found for {restaurant.Name}: {svRestaurantMenuInfos.Count} menus");

                                foreach (var svRestaurantMenu in svRestaurantMenuInfos)
                                {
                                    try
                                    {
                                        svRestaurantMenu.Menu.RestaurantId = restaurant.RestaurantId;// Link the menu to restaurant

                                        var menuImage = FoodHelper.GetImageForFood(svRestaurantMenu.Menu);

                                        // Set image
                                        if (menuImage != null)
                                            svRestaurantMenu.Menu.MenuImageId = menuImage.MenuImageId;

                                        var dbMenu = FoodDBManager.CreateMenu(svRestaurantMenu.Menu);

                                        // Link menu with alergies
                                        foreach (var alergyId in svRestaurantMenu.AlergyIds)
                                        {
                                            FoodDBManager.CreateMenuAlergy(svRestaurantMenu.Menu.MenuId, alergyId);
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                                break;

                            // ZFV Restaurant handler
                            case RestaurantLocation.UZH_Zentrum:
                            case RestaurantLocation.UZH_Irchel:

                                var uzhMenuInfos = FoodHelper.GetUzhMenus(FoodHelper.GetUZHDayOfTheWeek(), restaurant.InternalName);
                                await Context.Channel.SendMessageAsync($"Found for {restaurant.Name}: {uzhMenuInfos.Count} menus");

                                foreach (var uzhMenuInfo in uzhMenuInfos)
                                {
                                    try
                                    {
                                        uzhMenuInfo.Menu.RestaurantId = restaurant.RestaurantId;// Link the menu to restaurant

                                        var menuImage = FoodHelper.GetImageForFood(uzhMenuInfo.Menu);

                                        // Set image
                                        if (menuImage != null)
                                            uzhMenuInfo.Menu.MenuImageId = menuImage.MenuImageId;

                                        var dbMenu = FoodDBManager.CreateMenu(uzhMenuInfo.Menu);

                                        // Link menu with alergies
                                        foreach (var alergyId in uzhMenuInfo.AlergyIds)
                                        {
                                            FoodDBManager.CreateMenuAlergy(dbMenu.MenuId, alergyId);
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                                break;
                            default:
                                break;
                        }

                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"{restaurant.Name} is closed");
                    }
                }
                await Context.Channel.SendMessageAsync($"Done");
            }
            private static GoogleEngine Google = new GoogleEngine();

            */
        }
    }
}
