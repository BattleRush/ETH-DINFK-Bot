using ETHBot.DataLayer.Data.Discord;
using ETHDINFKBot.Data;
using ETHDINFKBot.Enums;
using ETHDINFKBot.Modules;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ETHDINFKBot
{
    public class PlaceWebsocket : WebSocketBehavior
    {
        public PlaceWebsocket()
        {
            //SendRandomBlocks();
        }
        public void SendPixel(short x, short y, Color color)
        {
            if (Sessions != null)
            {
                byte[] data = new byte[8];

                byte[] xBytes = BitConverter.GetBytes(x);
                byte[] yBytes = BitConverter.GetBytes(y);

                data[0] = 3; // identifier

                data[1] = xBytes[0];
                data[2] = xBytes[1];
                data[3] = yBytes[0];
                data[4] = yBytes[1];

                data[5] = color.R;
                data[6] = color.G;
                data[7] = color.B;

                Console.WriteLine($"Send: {x}/{y} paint R:{color.R}|G:{color.G}|B:{color.B}");

                Sessions.Broadcast(data);
            }
        }
        private async Task SendRandomBlocks()
        {
            Random r = new Random();
            while (true)
            {
                if (Sessions != null)
                {
                    byte[] data = new byte[7];

                    short randomX = (short)r.Next(0, 1000);
                    short randomY = (short)r.Next(0, 1000);


                    byte[] xBytes = BitConverter.GetBytes(randomX);
                    byte[] yBytes = BitConverter.GetBytes(randomY);


                    byte randomR = (byte)r.Next(0, 256);
                    byte randomG = (byte)r.Next(0, 256);
                    byte randomB = (byte)r.Next(0, 256);


                    data[0] = xBytes[0];
                    data[1] = xBytes[1];
                    data[2] = yBytes[0];
                    data[3] = yBytes[1];

                    data[4] = randomR;
                    data[5] = randomG;
                    data[6] = randomB;


                    Console.WriteLine($"Send: {randomX}/{randomY} paint R:{randomR}|G:{randomG}|B:{randomB}");

                    Sessions.Broadcast(data);
                }


                await Task.Delay(250);
            }
        }
        private byte[] GetFullImageResponse()
        {
            int size = 1000;

            byte[] response = new byte[1 + size * size * 3];
            response[0] = (byte)MessageEnum.FullImage_Response; // response for image

            int index = 1;
            if (PlaceModule.CurrentPlaceBitmap == null)
                return response;

            try
            {
                Bitmap cloneBitmap = PlaceModule.CurrentPlaceBitmap.Clone(new Rectangle(new Point(0, 0), new Size(size, size)), PlaceModule.CurrentPlaceBitmap.PixelFormat);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Color c = cloneBitmap.GetPixel(x, y);
                        response[index] = c.R;
                        response[index + 1] = c.G;
                        response[index + 2] = c.B;
                        index += 3;
                    }
                }
            }
            catch (Exception ex)
            {
                return null; // TODO logs
            }

            return response;
        }

        private byte[] GetTotalPixelCount()
        {
            byte[] returnData = new byte[5];
            returnData[0] = (byte)MessageEnum.TotalPixelCount_Response;

            PlaceDBManager dbManager = PlaceDBManager.Instance();

            // current limit 2.147 B pixels
            var totalPixelsPlaced = Convert.ToInt32(dbManager.GetBoardHistoryCount());

            byte[] pixelAmountBytes = BitConverter.GetBytes(totalPixelsPlaced);
            returnData[1] = pixelAmountBytes[0];
            returnData[2] = pixelAmountBytes[1];
            returnData[3] = pixelAmountBytes[2];
            returnData[4] = pixelAmountBytes[3];

            return returnData;
        }

        private byte[] GetTotalChunks()
        {
            var chunkFolder = Path.Combine(Program.BasePath, "TimelapseChunks");

            var fileAmount = Directory.GetFiles(chunkFolder).Length;

            byte[] returnData = new byte[3];
            returnData[0] = (byte)MessageEnum.TotalChunksAvailable_Response;


            byte[] chunkAmountBytes = BitConverter.GetBytes(fileAmount);
            returnData[1] = chunkAmountBytes[0];
            returnData[2] = chunkAmountBytes[1];

            return returnData;
        }
        private byte[] GetChunk(short chunkId)
        {
            var chunkFolder = Path.Combine(Program.BasePath, "TimelapseChunks");

            string file = $"Chunk_{chunkId}.dat";
            string filePath = Path.Combine(chunkFolder, file);
            var bytes = File.ReadAllBytes(filePath);
            //Console.WriteLine($"Loaded chunk {chunkId} with {bytes.Length.ToString("N0")} byte(s)");

            // todo maybe done bake the response id into the file
            return bytes;
        }

        private byte[] GetUserImageBytes(short userId)
        {
            var placeUser = PlaceModule.PlaceDiscordUsers.SingleOrDefault(i => i.PlaceDiscordUserId == userId);
            if (placeUser == null)
                return new byte[1];

            DatabaseManager dbManager = DatabaseManager.Instance();
            var discordUser = dbManager.GetDiscordUserById(placeUser.DiscordUserId);

            using (var webClient = new WebClient())
            {
                try
                {
                    var imageBytes = webClient.DownloadData(discordUser.AvatarUrl);

                    byte[] returnData = new byte[3 + imageBytes.Length];
                    returnData[0] = (byte)MessageEnum.GetUserProfileImage_Response;

                    byte[] userIdBytes = BitConverter.GetBytes(userId);
                    returnData[1] = userIdBytes[0];
                    returnData[2] = userIdBytes[1];

                    for (int i = 0; i < imageBytes.Length; i++)
                        returnData[3 + i] = imageBytes[i];

                    return returnData;
                }
                catch (Exception ex)
                {
                    return new byte[1];
                }
            }
        }

        private byte[] GetFullUserInfos()
        {
            try
            {
                PlaceDBManager placeDbManager = PlaceDBManager.Instance();
                DatabaseManager dbManager = DatabaseManager.Instance();

                var discordUserInfos = new List<DiscordUser>();
                short userCount = Convert.ToInt16(PlaceModule.PlaceDiscordUsers.Count);

                // TODO Load with PlaceDiscordUsers the DiscordUser table aswel
                // todo do with 1 query only -> faster
                foreach (var user in PlaceModule.PlaceDiscordUsers)
                    discordUserInfos.Add(dbManager.GetDiscordUserById(user.DiscordUserId));

                /// 0 | ID
                /// 1-2 | Amount of users loaded
                /// USER REPEAT (rel) Total 200
                /// 0-1 | user id (int 16)
                /// 2-97 Username (utf8 3 bytes per char)
                /// 98-197 | Url of the Profile (10 chars around spare) ASCII 1 byte per char
                /// 198 | IsBot 1 byte (could move 1 bit to user id as we wont need all 16 bits but me lazy)
                byte[] response = new byte[3 + userCount * (2 + 32 * 3 + 100 + 1)];

                response[0] = (byte)MessageEnum.GetUsers_Response;


                byte[] userAmountBytes = BitConverter.GetBytes(userCount);
                response[1] = userAmountBytes[0];
                response[2] = userAmountBytes[1];

                int index = 3;

                foreach (var discordUser in discordUserInfos)
                {
                    short userId = PlaceModule.PlaceDiscordUsers.Single(i => i.DiscordUserId == discordUser.DiscordUserId).PlaceDiscordUserId;
                    string name = discordUser.Nickname ?? discordUser.Username;
                    string url = discordUser.AvatarUrl ?? "";

                    byte[] userIdBytes = BitConverter.GetBytes(userId);
                    var nameBytes = Encoding.UTF8.GetBytes(name);
                    var urlBytes = Encoding.ASCII.GetBytes(url);

                    response[index] = userIdBytes[0];
                    response[index + 1] = userIdBytes[1];
                    index += 2;

                    nameBytes.CopyTo(response, index);
                    index += 32 * 3; // TODO verify this is indeed enough

                    urlBytes.CopyTo(response, index);
                    index += 100; // TODO verify this is indeed enough

                    response[index] = (byte)(discordUser.IsBot ? 1 : 0);
                    index++;
                }
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return new byte[1];
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var data = e.RawData;

            byte packetId = data[0];

            try
            {
                switch ((MessageEnum)packetId)
                {
                    case MessageEnum.FullImage_Request:
                        var fullImageBytes = GetFullImageResponse();
                        Send(fullImageBytes);
                        break;

                    case MessageEnum.TotalPixelCount_Request:
                        var totalPixelCountResponse = GetTotalPixelCount();

                        Send(totalPixelCountResponse);
                        break;

                    case MessageEnum.TotalChunksAvailable_Request:
                        var totalChunkResponse = GetTotalChunks();

                        Send(totalChunkResponse);
                        break;

                    case MessageEnum.GetChunk_Request:
                        //Console.WriteLine("Received GetChunk_Request");
                        byte[] chunkIdBytes = data.Skip(1).Take(2).ToArray();
                        short chunkId = BitConverter.ToInt16(chunkIdBytes, 0);

                        var chunkBytes = GetChunk(chunkId);

                        Send(chunkBytes);

                        //Console.WriteLine("SEND GetChunk_Request");
                        break;

                    case MessageEnum.GetUsers_Request:
                        var userBytes = GetFullUserInfos();

                        Send(userBytes);
                        break;

                    case MessageEnum.GetUserProfileImage_Request:

                        byte[] userIdBytes = data.Skip(1).Take(2).ToArray();
                        short userId = BitConverter.ToInt16(userIdBytes, 0);
                        var userImageBytes = GetUserImageBytes(userId);

                        Send(userImageBytes);
                        break;

                    case MessageEnum.FullImage_Response:
                    case MessageEnum.LivePixel:
                    case MessageEnum.TotalPixelCount_Response:
                    case MessageEnum.TotalChunksAvailable_Response:
                    case MessageEnum.GetChunk_Response:
                    case MessageEnum.GetUsers_Response:
                    case MessageEnum.GetUserProfileImage_Response:
                        // this case shouldnt happen and we simply ignore it for now
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnMessage error: " + ex.ToString());
            }
            //var msg = System.Text.Encoding.UTF8.GetString(e.RawData);
            //Console.WriteLine("Got Message: " + msg);
        }
    }
}
