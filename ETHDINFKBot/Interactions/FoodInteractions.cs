using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Enums;
using ETHBot.DataLayer.Data.ETH.Food;
using ETHDINFKBot.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Interactions
{
    // TODO alot of duplicate code to be removed
    public class FoodInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        private static FoodDBManager FoodDBManager = FoodDBManager.Instance();
        [SlashCommand("food", "Retreived current food info")]
        public async Task ThingsAsync()
        {

        }

        [ComponentInteraction("food-fav-main")]
        public async Task FoodMain()
        {

            var message = Context.Interaction as SocketMessageComponent;
            var user = Context.Interaction.User;

            // TODO Find better solution for this
            if (message.Message.Embeds.First().Author.Value.Name != $"{user.Username}#{user.Discriminator}")
            {
                await Context.Interaction.RespondAsync($"This isnt your setting. Call it with **{Program.CurrentPrefix}food fav** to change your settings", ephemeral: true);
                return;
            }

            await Context.Interaction.DeferAsync();

            // copy of .food fav command
            var currentUserId = user.Id;


            var userMenuSetting = FoodDBManager.GetUserFoodSettings(currentUserId);
            var userFavRestaurants = FoodDBManager.GetUsersFavouriteRestaurants(currentUserId);
            var availableRestaurants = FoodDBManager.GetAllRestaurants();


            if (userMenuSetting == null)
                userMenuSetting = new MenuUserSetting();

            // Get Current settings
            // Get Current faved restaurants



            var builderComponent = new ComponentBuilder();

            try
            {
                builderComponent.WithButton("Filter Vegetarian", $"food-fav-setting-vegetarian", userMenuSetting.VegetarianPreference ? ButtonStyle.Primary : ButtonStyle.Danger, Emote.Parse($"<:food_vegetarian:1017751739648188487>"), null, false, 0);
                builderComponent.WithButton("Filter Vegan", $"food-fav-setting-vegan", userMenuSetting.VeganPreference ? ButtonStyle.Primary : ButtonStyle.Danger, Emote.Parse($"<:food_vegan:1017751741455937536>"), null, false, 0);
                builderComponent.WithButton("Show all nutritions stats", $"food-fav-setting-nutritions", userMenuSetting.FullNutritions ? ButtonStyle.Primary : ButtonStyle.Danger, null/*Emote.Parse($"<:food_vegan:1017751741455937536>")*/, null, false, 0);
                builderComponent.WithButton("Show Allergies", $"food-fav-setting-allergies", userMenuSetting.DisplayAllergies ? ButtonStyle.Primary : ButtonStyle.Danger, null/*Emote.Parse($"<:food_vegan:1017751741455937536>")*/, null, false, 0);

                var favedRestaurantIds = userFavRestaurants.Select(i => i.RestaurantId);

                int row = 1;

                // iterate trough elements of RestaurantLocation enum

                foreach (var location in Enum.GetValues(typeof(RestaurantLocation)).Cast<RestaurantLocation>())
                {
                    var locationDisplayName = location.GetType().GetMember(location.ToString()).First().GetCustomAttribute<DisplayAttribute>();

                    switch (location)
                    {
                        case RestaurantLocation.ETH_UZH_Zentrum:
                            row = 1;
                            break;
                        case RestaurantLocation.ETH_Hoengg:
                            row = 1;
                            break;
                        case RestaurantLocation.UZH_Irchel_Oerlikon:
                            row = 1;
                            break;
                        case RestaurantLocation.Zurich:
                            row = 2;
                            break;
                        case RestaurantLocation.HSLU:
                            row = 1;
                            break;
                        case RestaurantLocation.Bern:
                            row = 2;
                            break;
                        case RestaurantLocation.Other:
                            row = 3;
                            break;
                    }

                    int locationValue = (int)location;

                    builderComponent.WithButton(locationDisplayName.Name, $"food-fav-location-{locationValue}", ButtonStyle.Success, null, null, false, row);
                }


                /*foreach (var restaurantLocationGroup in availableRestaurants.GroupBy(i => i.Location))
                {
                    // Currently only supports only 4 locations with 5 restaurants each

                    foreach (var restaurant in restaurantLocationGroup)
                    {
                        builderComponent.WithButton(restaurant.Name, $"food-fav-{restaurant.RestaurantId}", favedRestaurantIds.Contains(restaurant.RestaurantId) ? ButtonStyle.Primary : ButtonStyle.Danger, null, null, false, row);
                    }

                    row++;
                }*/

                await message.Message.ModifyAsync(i =>
            {
                //i.Attachments = attachments;
                //i.Embed = builder.Build();
                //i.Content = emoteResult.textBlock;
                i.Components = builderComponent.Build();
            });
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
            }


        }


        [ComponentInteraction("food-fav-location-*")]
        public async Task FoodFavouriteLocation(string favChange)
        {
            var message = Context.Interaction as SocketMessageComponent;
            var user = Context.Interaction.User;

            // TODO Find better solution for this
            if (message.Message.Embeds.First().Author.Value.Name != $"{user.Username}#{user.Discriminator}")
            {
                await Context.Interaction.RespondAsync($"This isnt your setting. Call it with **{Program.CurrentPrefix}food fav** to change your settings", ephemeral: true);
                return;
            }

            await Context.Interaction.DeferAsync();

            // TODO Check if updates successfull 
            if (int.TryParse(favChange, out int locationId))
            {
                RestaurantLocation restaurantLocation = (RestaurantLocation)locationId;

                // get all restaurants for this location
                var restaurants = FoodDBManager.GetAllRestaurants().Where(i => i.Location == restaurantLocation);


                var userMenuSetting = FoodDBManager.GetUserFoodSettings(user.Id);
                var userFavRestaurants = FoodDBManager.GetUsersFavouriteRestaurants(user.Id);
                var availableRestaurants = FoodDBManager.GetAllRestaurants();


                if (userMenuSetting == null)
                    userMenuSetting = new ETHBot.DataLayer.Data.ETH.Food.MenuUserSetting();

                var builderComponent = new ComponentBuilder();

                // display buttons for all restaurants for this location and offer back to main menu

                try
                {
                    builderComponent.WithButton("Back to main menu", $"food-fav-main", ButtonStyle.Success, null, null, false, 0);

                    int row = 1;
                    int columns = 0;
                    foreach (var restaurant in restaurants)
                    {
                        builderComponent.WithButton(restaurant.Name, $"food-fav-setting-{restaurant.RestaurantId}", userFavRestaurants.Select(i => i.RestaurantId).Contains(restaurant.RestaurantId) ? ButtonStyle.Primary : ButtonStyle.Danger, null, null, false, row);

                        columns++;
                        if (columns == 5)
                        {
                            row++;
                            columns = 0;
                        }
                    }
                }
                catch (Exception ex)
                {

                }

                await message.Message.ModifyAsync(i =>
                {
                    //i.Attachments = attachments;
                    //i.Embed = builder.Build();
                    //i.Content = emoteResult.textBlock;
                    i.Components = builderComponent.Build();
                });
            }
        }

        [ComponentInteraction("food-fav-setting-*")]
        public async Task FoodFavouriteChange(string favChange)
        {
            var message = Context.Interaction as SocketMessageComponent;
            var user = Context.Interaction.User;

            // TODO Find better solution for this
            if (message.Message.Embeds.First().Author.Value.Name != $"{user.Username}#{user.Discriminator}")
            {
                await Context.Interaction.RespondAsync($"This isnt your setting. Call it with **{Program.CurrentPrefix}food fav** to change your settings", ephemeral: true);
                return;
            }

            //await Context.Interaction.DeferAsync();

            // TODO Check if updates successfull 
            if (int.TryParse(favChange, out int restaurantId))
            {
                await Context.Interaction.DeferAsync();
                
                // Restaurant fav change
                var returnedFavRestaurant = FoodDBManager.GetUsersFavouriteRestaurant(user.Id, restaurantId);
                if (returnedFavRestaurant != null)
                {
                    // Delete the entry
                    FoodDBManager.DeleteUsersFavouriteRestaurant(user.Id, restaurantId);
                }
                else
                {
                    FoodDBManager.CreateUsersFavouriteRestaurant(user.Id, restaurantId);
                }
                var locationId = FoodDBManager.GetRestaurantById(restaurantId).Location;
                UpdateFootPageForLocation((int)locationId);
            }
            else
            {
                var userMenuSetting = FoodDBManager.GetUserFoodSettings(user.Id);
                if (userMenuSetting == null)
                {
                    userMenuSetting = new ETHBot.DataLayer.Data.ETH.Food.MenuUserSetting()
                    {
                        DiscordUserId = user.Id
                    };
                }

                switch (favChange)
                {
                    case "vegetarian":
                        userMenuSetting.VegetarianPreference = !userMenuSetting.VegetarianPreference;
                        break;
                    case "vegan":
                        userMenuSetting.VeganPreference = !userMenuSetting.VeganPreference;
                        break;
                    case "nutritions":
                        userMenuSetting.FullNutritions = !userMenuSetting.FullNutritions;
                        break;
                    case "allergies":
                        userMenuSetting.DisplayAllergies = !userMenuSetting.DisplayAllergies;
                        break;
                    default:
                        break;
                }

                // allow only one of the two
                if(userMenuSetting.VeganPreference && userMenuSetting.VegetarianPreference)
                {
                    userMenuSetting.VegetarianPreference = false;

                    // send user message why this is not allowed
                    await Context.Interaction.RespondAsync($"You can't have both vegan and vegetarian filter on (It's an AND filter). I turned off vegetarian for you :)", ephemeral: true);
                }
                else
                {
                    await Context.Interaction.DeferAsync();
                }

                var updatedRecord = FoodDBManager.UpdateUserFoodSettings(userMenuSetting);


                UpdateFoodFavPage();
            }
        }

        private async void UpdateFootPageForLocation(int locationId)
        {
            var message = Context.Interaction as SocketMessageComponent;
            var user = Context.Interaction.User;

            var userMenuSetting = FoodDBManager.GetUserFoodSettings(user.Id);
            var userFavRestaurants = FoodDBManager.GetUsersFavouriteRestaurants(user.Id);
            var availableRestaurants = FoodDBManager.GetAllRestaurants();


            if (userMenuSetting == null)
                userMenuSetting = new ETHBot.DataLayer.Data.ETH.Food.MenuUserSetting();





            RestaurantLocation restaurantLocation = (RestaurantLocation)locationId;

            // get all restaurants for this location
            var restaurants = FoodDBManager.GetAllRestaurants().Where(i => i.Location == restaurantLocation);




            if (userMenuSetting == null)
                userMenuSetting = new ETHBot.DataLayer.Data.ETH.Food.MenuUserSetting();

            var builderComponent = new ComponentBuilder();

            // display buttons for all restaurants for this location and offer back to main menu

            try
            {
                builderComponent.WithButton("Back to main menu", $"food-fav-main", ButtonStyle.Success, null, null, false, 0);

                int row = 1;
                int columns = 0;
                foreach (var restaurant in restaurants)
                {
                    builderComponent.WithButton(restaurant.Name, $"food-fav-setting-{restaurant.RestaurantId}", userFavRestaurants.Select(i => i.RestaurantId).Contains(restaurant.RestaurantId) ? ButtonStyle.Primary : ButtonStyle.Danger, null, null, false, row);

                    columns++;
                    if (columns == 5)
                    {
                        row++;
                        columns = 0;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            await message.Message.ModifyAsync(i =>
            {
                //i.Attachments = attachments;
                //i.Embed = builder.Build();
                //i.Content = emoteResult.textBlock;
                i.Components = builderComponent.Build();
            });
        }

        private async void UpdateFoodFavPage()
        {
            var message = Context.Interaction as SocketMessageComponent;
            var user = Context.Interaction.User;

            var userMenuSetting = FoodDBManager.GetUserFoodSettings(user.Id);
            var userFavRestaurants = FoodDBManager.GetUsersFavouriteRestaurants(user.Id);
            var availableRestaurants = FoodDBManager.GetAllRestaurants();


            if (userMenuSetting == null)
                userMenuSetting = new ETHBot.DataLayer.Data.ETH.Food.MenuUserSetting();

            var builderComponent = new ComponentBuilder();

            try
            {
                builderComponent.WithButton("Filter Vegetarian", $"food-fav-setting-vegetarian", userMenuSetting.VegetarianPreference ? ButtonStyle.Primary : ButtonStyle.Danger, Emote.Parse($"<:food_vegetarian:1017751739648188487>"), null, false, 0);
                builderComponent.WithButton("Filter Vegan", $"food-fav-setting-vegan", userMenuSetting.VeganPreference ? ButtonStyle.Primary : ButtonStyle.Danger, Emote.Parse($"<:food_vegan:1017751741455937536>"), null, false, 0);
                builderComponent.WithButton("Show all nutritions stats", $"food-fav-setting-nutritions", userMenuSetting.FullNutritions ? ButtonStyle.Primary : ButtonStyle.Danger, null/*Emote.Parse($"<:food_vegan:1017751741455937536>")*/, null, false, 0);
                builderComponent.WithButton("Show Allergies", $"food-fav-setting-allergies", userMenuSetting.DisplayAllergies ? ButtonStyle.Primary : ButtonStyle.Danger, null/*Emote.Parse($"<:food_vegan:1017751741455937536>")*/, null, false, 0);


                var favedRestaurantIds = userFavRestaurants.Select(i => i.RestaurantId);

                int row = 1;

                // iterate trough elements of RestaurantLocation enum

                foreach (var location in Enum.GetValues(typeof(RestaurantLocation)).Cast<RestaurantLocation>())
                {
                    var locationDisplayName = location.GetType().GetMember(location.ToString()).First().GetCustomAttribute<DisplayAttribute>();

                    switch (location)
                    {
                        case RestaurantLocation.ETH_UZH_Zentrum:
                            row = 1;
                            break;
                        case RestaurantLocation.ETH_Hoengg:
                            row = 1;
                            break;
                        case RestaurantLocation.UZH_Irchel_Oerlikon:
                            row = 1;
                            break;
                        case RestaurantLocation.Zurich:
                            row = 2;
                            break;
                        case RestaurantLocation.HSLU:
                            row = 1;
                            break;
                        case RestaurantLocation.Bern:
                            row = 2;
                            break;
                        case RestaurantLocation.Other:
                            row = 3;
                            break;
                    }

                    int locationValue = (int)location;

                    builderComponent.WithButton(locationDisplayName.Name, $"food-fav-location-{locationValue}", ButtonStyle.Success, null, null, false, row);
                }

                /*
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
                }*/
            }
            catch (Exception ex)
            {

            }

            await message.Message.ModifyAsync(i =>
            {
                //i.Attachments = attachments;
                //i.Embed = builder.Build();
                //i.Content = emoteResult.textBlock;
                i.Components = builderComponent.Build();
            });
        }
    }
}
