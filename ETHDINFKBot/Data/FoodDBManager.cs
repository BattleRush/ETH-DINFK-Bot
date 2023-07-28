using ETHBot.DataLayer;
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

        public void AddFood2050CO2Entry(Food2050CO2Entry entry)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                if(entry.CO2Delta >= 0)
                    return;

                context.Food2050CO2Entries.Add(entry);
                context.SaveChanges();
            }
        }

        public List<Restaurant> GetAllFood2050Restaurants()
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.Restaurants.Where(i => i.IsFood2050Supported).ToList();
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

        public string GetMenuAllergiesString(Menu menu, bool en = true)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                var menuAllergyIds = context.MenuAllergies.Where(i => i.Menu == menu).Select(i => i.AllergyId).ToList();
                var Allergies = context.Allergies.Where(i => menuAllergyIds.Contains(i.AllergyId));

                string AllergyString = string.Join(", ", Allergies.Select(i => i.Name));
                if (!en)
                    AllergyString = string.Join(", ", Allergies.Select(i => i.NameDE));

                return AllergyString;
            }

            return "";
        }

        public List<int> GetMenuAllergyIds(Menu menu, bool en = true)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.MenuAllergies.Where(i => i.MenuId == menu.MenuId).Select(i => i.AllergyId).ToList();
            }
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

        public List<Menu> GetMenusByDay(DateTime datetime, int restaurantId = -1)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    if (restaurantId < 0)
                        return context.Menus.Where(i => i.DateTime.Date == datetime.Date).ToList();
                    else
                        return context.Menus.Where(i => i.DateTime.Date == datetime.Date && i.RestaurantId == restaurantId).ToList();
                }
                catch (Exception ex)
                {

                }
            }

            return null;
        }

        public Menu GetMenusById(int menuId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    return context.Menus.SingleOrDefault(i => i.MenuId == menuId);
                }
                catch (Exception ex)
                {
                    return null;
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
                    // Clear any menu Allergies first
                    var menuAlregies = context.MenuAllergies.Where(i => i.MenuId == menu.MenuId);
                    if (menuAlregies.Any())
                    {
                        context.MenuAllergies.RemoveRange(menuAlregies);
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


        public Allergy CreateAllergy(Allergy Allergy)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    if (!context.Allergies.Any(i => i.AllergyId == Allergy.AllergyId))
                        context.Allergies.Add(Allergy);

                    context.SaveChanges();
                }
                catch (Exception ex)
                {

                }
            }

            return Allergy;
        }


        public bool CreateMenuAllergy(int menuId, int AllergyId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                try
                {
                    context.MenuAllergies.Add(new MenuAllergy()
                    {
                        MenuId = menuId,
                        AllergyId = AllergyId
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

        public List<DiscordUserFavouriteRestaurant> GetUsersFavouriteRestaurants(ulong discordUserId)
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.DiscordUserFavouriteRestaurants.Where(i => i.DiscordUserId == discordUserId).ToList();
            }
        }

        public bool CreateUsersFavouriteRestaurant(ulong discordUserId, int restaurantId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordUserFavouriteRestaurants.Add(new DiscordUserFavouriteRestaurant()
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
                    var returnedFavRestaurant = context.DiscordUserFavouriteRestaurants
                        .SingleOrDefault(i => i.DiscordUserId == discordUserId && i.RestaurantId == restaurantId);

                    if (returnedFavRestaurant != null)
                    {
                        context.DiscordUserFavouriteRestaurants.Remove(returnedFavRestaurant);
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

        public DiscordUserFavouriteRestaurant GetUsersFavouriteRestaurant(ulong discordUserId, int restaurantId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordUserFavouriteRestaurants.SingleOrDefault(i => i.DiscordUserId == discordUserId && i.RestaurantId == restaurantId);
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
                    return context.MenuImages.Where(i => i.ImageSearchTerm == searchTerm && i.Language == language && i.Available && i.Enabled).ToList();
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
                    return context.Menus.Where(i => i.RestaurantId == restaurantId && i.DateTime.Date == datetime.Date).ToList();
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

        public bool SetImageIdForMenu(int menuId, int imageId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbMenu = context.Menus.SingleOrDefault(i => i.MenuId == menuId);

                    if (dbMenu != null)
                    {
                        dbMenu.MenuImageId = imageId;
                        context.SaveChanges();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return false;
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
                            Language = language,
                            Available = true,
                            Enabled = true
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
        public List<Allergy> GetAllergies()
        {
            using (ETHBotDBContext context = new ETHBotDBContext())
            {
                return context.Allergies.ToList();
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
                    dbMenuUserSetting.FullNutritions = menuUserSetting.FullNutritions;
                    dbMenuUserSetting.DisplayAllergies = menuUserSetting.DisplayAllergies;
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
