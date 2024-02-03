using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Fun;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Data
{
    public class FileDBManager
    {
        private static FileDBManager _instance;
        private static object syncLock = new object();
        private readonly ILogger _logger = new Logger<FileDBManager>(Program.Logger);

        public static FileDBManager Instance()
        {
            lock (syncLock)
            {
                if (_instance == null)
                {
                    _instance = new FileDBManager();
                }
            }

            return _instance;
        }

        
        public List<DiscordFile> GetDiscordFile(ulong messageId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordFiles.Where(i => i.DiscordMessageId == messageId).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordFile GetDiscordFileById(int fileId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordFiles.SingleOrDefault(i => i.DiscordFileId == fileId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        // unsure how reliable this will be because discord links will change
        public DiscordFile GetDiscordFileByUrl(string url)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordFiles.SingleOrDefault(i => i.UrlWithoutParams == url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        // ocr boxes by file id
        public List<OcrBox> GetOcrBoxesByFileId(int fileId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.OcrBoxes.Where(i => i.DiscordFileId == fileId).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<PytorchModel> GetPytorchModels()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PytorchModels.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<PytorchModel> GetPytorchModels(bool active = true)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PytorchModels.Where(i => i.Active == active).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public PytorchModel GetImagePytorchModels()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PytorchModels.SingleOrDefault(i => i.ForImage == true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public PytorchModel GetVideoPytorchModels()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PytorchModels.SingleOrDefault(i => i.ForVideo == true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public PytorchModel GetAudioPytorchModels()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PytorchModels.SingleOrDefault(i => i.ForAudio == true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordFileEmbeds GetDiscordFileEmbeds(int fileId, int modelId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordFileEmbeds.SingleOrDefault(i => i.DiscordFileId == fileId && i.PytorchModelId == modelId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public bool SaveDiscordFile(DiscordFile file)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordFiles.Add(file);
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

        public bool SaveDiscordFileEmbeds(DiscordFileEmbeds fileEmbeds)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordFileEmbeds.Add(fileEmbeds);
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

        public bool SaveOcrBox(OcrBox ocrBox)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.OcrBoxes.Add(ocrBox);
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

        public bool SaveOcrBoxes(List<OcrBox> ocrBoxes)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.OcrBoxes.AddRange(ocrBoxes);
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

        public bool SavePytorchModel(PytorchModel model)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.PytorchModels.Add(model);
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

        public bool UpdatePytorchModel(PytorchModel model)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.PytorchModels.Update(model);
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

        public bool UpdateDiscordFile(DiscordFile file)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordFiles.Update(file);
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

        public bool UpdateDiscordFileEmbeds(DiscordFileEmbeds fileEmbeds)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.DiscordFileEmbeds.Update(fileEmbeds);
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

        public List<DiscordFile> GetFilesToOcrProcess(int count = 1000)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordFiles.Where(i => i.OcrDone == false && i.IsImage && i.Extension != "gif").Take(count).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }
    }
}
