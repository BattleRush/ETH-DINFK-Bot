﻿using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.ETH.Food;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Data
{
    public class FoodDBManager
    {
        private static FoodDBManager _instance;
        private static object syncLock = new object();
        private readonly ILogger _logger = new Logger<FoodDBManager>(Program.Logger);

        public static FoodDBManager Instance()
        {
            lock (syncLock)
            {
                if (_instance == null)
                {
                    _instance = new FoodDBManager();
                }
            }

            return _instance;
        }


        // TODO Better name handling 
        public Restaurant GetRestaurantByName(string name)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.Restaurants.FirstOrDefault(i => i.Name == name);
            }
        }

        public List<Restaurant> GetAllRestaurants()
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.Restaurants.ToList();
            }
        }

        public Restaurant GetRestaurantByInternalName(string internalName)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.Restaurants.FirstOrDefault(i => i.InternalName == internalName);
            }
        }

        public Restaurant GetRestaurantById(int restaurantId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.Restaurants.FirstOrDefault(i => i.RestaurantId == restaurantId);
            }
        }

        public Restaurant CreateRestaurant(Restaurant restaurant)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                context.Restaurants.Add(restaurant);
                context.SaveChanges();
            }

            return restaurant;
        }

        public bool DeleteRestaurant(Restaurant restaurant)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    var dbRestaurant = context.Restaurants.FirstOrDefault(i => i.RestaurantId == restaurant.RestaurantId);
                    if (dbRestaurant != null)
                        context.Restaurants.Remove(dbRestaurant);
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            return true;
        }

        public List<Menu> GetMenusByDay(DateTime datetime)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    return context.Menus.Where(i => i.DateTime.Date == datetime.Date).ToList();;
                }
                catch (Exception ex)
                {
                    
                }
            }

            return null;
        }

        public bool DeleteMenu(Menu menu)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    // Clear any menu alergies first
                    var menuAlregies = context.MenuAlergies.Where(i => i.MenuId == menu.MenuId);
                    if (menuAlregies.Any())
                    {
                        context.MenuAlergies.RemoveRange(menuAlregies);
                        context.SaveChanges();
                    }

                    // Delete the menu
                    var dbMenu = context.Menus.FirstOrDefault(i => i.MenuId == menu.MenuId);
                    if (dbMenu != null)
                    {
                        context.Menus.Remove(dbMenu);
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            return true;
        }


        public Alergy CreateAlergy(Alergy alergy)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    if (!context.Alergies.Any(i => i.AlergyId == alergy.AlergyId))
                        context.Alergies.Add(alergy);

                    context.SaveChanges();
                }
                catch (Exception ex)
                {

                }
            }

            return alergy;
        }


        public bool CreateMenuAlergy(int menuId, int alergyId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    context.MenuAlergies.Add(new MenuAlergy()
                    {
                        MenuId = menuId,
                        AlergyId = alergyId
                    });

                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            return true;
        }

        public List<DiscordUserFavouriteRestaturant> GetUsersFavouriteRestaurants(ulong discordUserId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.DiscordUserFavouriteRestaturants.Where(i => i.DiscordUserId == discordUserId).ToList();
            }
        }

        public bool CreateUsersFavouriteRestaurant(ulong discordUserId, int restaurantId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordUserFavouriteRestaturants.Add(new DiscordUserFavouriteRestaturant()
                    {
                        DiscordUserId = discordUserId,
                        RestaurantId = restaurantId
                    });

                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteUsersFavouriteRestaurant(ulong discordUserId, int restaurantId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var returnedFavRestaurant = context.DiscordUserFavouriteRestaturants
                        .SingleOrDefault(i => i.DiscordUserId == discordUserId && i.RestaurantId == restaurantId);

                    if (returnedFavRestaurant != null)
                    {
                        context.DiscordUserFavouriteRestaturants.Remove(returnedFavRestaurant);
                        context.SaveChanges();

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return false;
        }

        public DiscordUserFavouriteRestaturant GetUsersFavouriteRestaurant(ulong discordUserId, int restaurantId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordUserFavouriteRestaturants.SingleOrDefault(i => i.DiscordUserId == discordUserId && i.RestaurantId == restaurantId);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public List<MenuImage> GetMenuImages(string searchTerm, string language)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.MenuImages.Where(i => i.ImageSearchTerm == searchTerm && i.Language == language).ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public MenuImage GetBestMenuImage(int menuImageId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.MenuImages.SingleOrDefault(i => i.MenuImageId == menuImageId);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<Menu> GetMenusFromRestaurant(int restaurantId, DateTime datetime)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.Menus.Where(i => i.RestaurantId == restaurantId && i.DateTime.Day == datetime.Day).ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public bool ClearTodaysMenu(int restaurantId)
        {
            throw new NotImplementedException();
        }

        public Menu CreateMenu(Menu menu)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // TODO Use different match? restaurantid, datetime and name maybe?
                    var dbMenu = context.Menus
                        .SingleOrDefault(i => i.RestaurantId == menu.RestaurantId && i.DateTime.Date == menu.DateTime.Date && i.Name == menu.Name);

                    if (dbMenu != null)
                    {
                        // Menu exists dont create a new entry (TODO decide if it should delete)
                        //return UpdateMenu(menu);
                        return dbMenu;
                    }
                    else
                    {
                        context.Menus.Add(menu);
                        context.SaveChanges();

                        return menu;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public Menu UpdateMenu(Menu menu)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbMenu = context.Menus.SingleOrDefault(i => i.MenuId == menu.MenuId);
                    if (dbMenu == null)
                    {
                        return CreateMenu(menu);
                    }
                    else
                    {
                        dbMenu.Amount = menu.Amount;
                        dbMenu.DateTime = menu.DateTime;
                        dbMenu.Description = menu.Description;
                        dbMenu.Name = menu.Name;
                        dbMenu.IsLocal = menu.IsLocal;
                        // TODO Rest me lazy

                        context.SaveChanges();

                        return dbMenu;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public List<MenuImage> CreateMenuImages(List<string> imageUrls, string searchTerm, string language)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    foreach (var imageUrl in imageUrls)
                    {
                        context.MenuImages.Add(new MenuImage()
                        {
                            MenuImageUrl = imageUrl,
                            ImageSearchTerm = searchTerm,
                            Language = language
                        });
                    }
                    context.SaveChanges();

                    return GetMenuImages(searchTerm, language);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public MenuUserSetting GetUserFoodSettings(ulong discordUserId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.MenuUserSettings.SingleOrDefault(i => i.DiscordUserId == discordUserId);
            }
        }

        public MenuUserSetting UpdateUserFoodSettings(MenuUserSetting menuUserSetting)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                var dbMenuUserSetting = context.MenuUserSettings.SingleOrDefault(i => i.DiscordUserId == menuUserSetting.DiscordUserId);

                if (dbMenuUserSetting == null)
                {
                    // Create record
                    return CreateUserFoodSettings(menuUserSetting);
                }
                else
                {
                    // Update record
                    dbMenuUserSetting.VegetarianPreference = menuUserSetting.VegetarianPreference;
                    dbMenuUserSetting.VeganPreference = menuUserSetting.VeganPreference;
                    context.SaveChanges();
                }
            }

            return GetUserFoodSettings(menuUserSetting.DiscordUserId);
        }

        public MenuUserSetting CreateUserFoodSettings(MenuUserSetting menuUserSetting)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                context.MenuUserSettings.Add(menuUserSetting);
                context.SaveChanges();
            }

            return GetUserFoodSettings(menuUserSetting.DiscordUserId);
        }

    }
}