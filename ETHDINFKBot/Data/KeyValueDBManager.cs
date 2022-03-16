using ETHBot.DataLayer;
using ETHBot.DataLayer.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Data
{
    public class KeyValueDBManager
    {
        private static KeyValueDBManager _instance;

        private static object syncLock = new object();
        private readonly ILogger _logger = new Logger<KeyValueDBManager>(Program.Logger);

        public static KeyValueDBManager Instance()
        {
            lock (syncLock)
            {
                if (_instance == null)
                {
                    _instance = new KeyValueDBManager();
                }
            }

            return _instance;
        }

        /// <summary>
        /// Checks if the Key is already stored in the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public bool Contains(string keyName)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.StoredKeyValuePairs.Any(i => i.Key == keyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }


        /// <summary>
        /// Add new KeyValuePair
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public T Add<T>(string keyName, T value) where T : IConvertible
        {
            //if (typeof(IConvertible).IsAssignableFrom(typeof(T)))
            //    throw new ArgumentException("Can't pass types that dont implement the IConvertible interface");

            if (Contains(keyName))
                throw new ArgumentException("Key already exists");

            StoredKeyValuePair storedKeyValuePair = new StoredKeyValuePair()
            {
                Key = keyName,
                Value = value.ToString(),
                Type = typeof(T).Name
            };

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.StoredKeyValuePairs.Add(storedKeyValuePair);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return default(T);
            }

            return Get<T>(keyName);
        }

        /// <summary>
        /// Add KeyValuePair (From Admin)
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public string Add(string keyName, string value, string type)
        {
            if (Contains(keyName))
                throw new ArgumentException("Key already exists");

            StoredKeyValuePair storedKeyValuePair = new StoredKeyValuePair()
            {
                Key = keyName,
                Value = value.ToString(),
                Type = type
            };

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.StoredKeyValuePairs.Add(storedKeyValuePair);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }

            return Get(keyName).Value;
        }


        /// <summary>
        /// Get KeyValuePair
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public T Get<T>(string keyName) where T : IConvertible
        {
            //if (typeof(IConvertible).IsAssignableFrom(typeof(T)))
            //    throw new ArgumentException("Can't pass types that don't implement the IConvertible interface");

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var storedKeyValuePair = context.StoredKeyValuePairs.AsQueryable().SingleOrDefault(i => i.Key == keyName);
                    if(storedKeyValuePair != null)
                        return (T)Convert.ChangeType(storedKeyValuePair.Value, typeof(T));
                    else
                        return default(T);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return default(T);
            }
        }

        public IEnumerable<string> GetKeys()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.StoredKeyValuePairs.Select(i => i.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public IEnumerable<StoredKeyValuePair> GetAll()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.StoredKeyValuePairs.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get KeyValuePair (Use when the Type is unknown or not important)
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public (string Key, string Value, string Type) Get(string keyName)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var storedKeyValuePair = context.StoredKeyValuePairs.AsQueryable().SingleOrDefault(i => i.Key == keyName);
                    return (storedKeyValuePair.Key, storedKeyValuePair.Value, storedKeyValuePair.Type);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return (null, null, null);
            }
        }

        /// <summary>
        /// Update KeyValuePair
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public T Update<T>(string keyName, T value) where T : IConvertible
        {
            //if (typeof(IConvertible).IsAssignableFrom(typeof(T)))
            //    throw new ArgumentException("Can't pass types that don't implement the IConvertible interface");

            // If the Key doesn't exist create the new KeyValuePair
            if (!Contains(keyName))
                return Add<T>(keyName, value);

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var storedKeyValuePair = context.StoredKeyValuePairs.AsQueryable().SingleOrDefault(i => i.Key == keyName);
                    storedKeyValuePair.Value = value.ToString();
                    storedKeyValuePair.Type = typeof(T).Name;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return default(T);
            }

            return Get<T>(keyName);
        }

        /// <summary>
        /// Update KeyValuePair (From admin)
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string Update(string keyName, string value, string type = null)
        {            
            // If the Key doesn't exist create the new KeyValuePair
            if (!Contains(keyName))
                return Add(keyName, value, type);

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var storedKeyValuePair = context.StoredKeyValuePairs.AsQueryable().SingleOrDefault(i => i.Key == keyName);
                    storedKeyValuePair.Value = value;

                    if(type != null)
                        storedKeyValuePair.Type = type;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }

            return Get(keyName).Value;
        }

        /// <summary>
        /// Delete the KeyValuePair
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public bool Delete(string keyName)
        {
            if (!Contains(keyName))
                throw new ArgumentException("Key does not exists");

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var storedKeyValuePair = context.StoredKeyValuePairs.AsQueryable().SingleOrDefault(i => i.Key == keyName);
                    context.StoredKeyValuePairs.Remove(storedKeyValuePair);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Delete the KeyValuePair, but return the last value stored
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keyName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public T Delete<T>(string keyName) where T : IConvertible
        {
            //if (typeof(IConvertible).IsAssignableFrom(typeof(T)))
            //    throw new ArgumentException("Can't pass types that don't implement the IConvertible interface");

            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var storedKeyValuePair = context.StoredKeyValuePairs.AsQueryable().SingleOrDefault(i => i.Key == keyName);
                    context.StoredKeyValuePairs.Remove(storedKeyValuePair);
                    context.SaveChanges();

                    return (T)Convert.ChangeType(storedKeyValuePair.Value, typeof(T));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return default(T);
            }
        }
    }
}
