using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ETHDINFKBot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Interactions
{
    public class FoodInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        private static FoodDBManager FoodDBManager = FoodDBManager.Instance();
        [SlashCommand("food", "Retreived current food info")]
        public async Task ThingsAsync()
        {

        }



        [ComponentInteraction("food-fav-*")]
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

            await Context.Interaction.DeferAsync();

            // TODO Check if updates successfull 
            if (int.TryParse(favChange, out int restaurantId))
            {
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

                var updatedRecord = FoodDBManager.UpdateUserFoodSettings(userMenuSetting);
            }



            UpdateFoodFavPage();
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

            }

            await message.Message.ModifyAsync(i => {
                //i.Attachments = attachments;
                //i.Embed = builder.Build();
                //i.Content = emoteResult.textBlock;
                i.Components = builderComponent.Build();
            });
        }
    }
}
