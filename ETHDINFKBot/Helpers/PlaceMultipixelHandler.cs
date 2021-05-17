using ETHBot.DataLayer.Data.Fun;
using ETHDINFKBot.Data;
using ETHDINFKBot.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Helpers
{
    public enum MultipixelJobStatus
    {
        None = 0,
        Importing = 1,
        Ready = 2,
        Active = 3,
        Done = 4,
        Canceled = 5
    }

    public class PlaceMultipixelHandler
    {
        public async Task<bool> MultiPixelProcess()
        {
            PlaceDBManager placeDBManager = PlaceDBManager.Instance();

            while (true)
            {
                var delay = Task.Delay(TimeSpan.FromSeconds(100));
                var verifiedPlaceUsers = placeDBManager.GetPlaceDiscordUsers(true);

                List<PlaceMultipixelJob> allActiveJobs = new List<PlaceMultipixelJob>();

                foreach (var placeUser in verifiedPlaceUsers)
                {
                    var activeJobs = placeDBManager.GetMultipixelJobs(placeUser.PlaceDiscordUserId);

                    var job = activeJobs.FirstOrDefault();

                    if (job == null)
                        continue; // the user has nothing queued up;

                    // set job status to Active
                    if (job.Status == (int)MultipixelJobStatus.Ready)
                        placeDBManager.UpdatePlaceMultipixelJobStatus(job.PlaceMultipixelJobId, MultipixelJobStatus.Active);

                    allActiveJobs.Add(job);
                }

                List<Task> tasks = new List<Task>();
                List<int> currentPacketIds = new List<int>();
                foreach (var job in allActiveJobs)
                {
                    var lastPacket = placeDBManager.GetNextFreeMultipixelJobPacket(job.PlaceMultipixelJobId);
                    currentPacketIds.Add(lastPacket.PlaceMultipixelPacketId);
                    tasks.Add(PlaceMultipixelPacket(lastPacket.Instructions, job.PlaceDiscordUserId));
                }

                await Task.WhenAll(tasks);

                foreach (var packetId in currentPacketIds)
                {
                    placeDBManager.MarkMultipixelJobPacketAsDone(packetId);
                }

                // chack if any job finished TODO maybe save in the job the count done
                foreach (var job in allActiveJobs)
                {
                    var lastPacket = placeDBManager.GetNextFreeMultipixelJobPacket(job.PlaceMultipixelJobId);
                    if(lastPacket == null)
                    {
                        // job is done
                        placeDBManager.UpdatePlaceMultipixelJobStatus(job.PlaceMultipixelJobId, MultipixelJobStatus.Done);
                    }
                }

                await delay; // Ensure 1 packet / user / 100 sec
            }

            return true;
        }


        private async Task<bool> PlaceMultipixelPacket(string instructions, short placeDiscordUserId)
        {
            PlaceDBManager placeDBManager = PlaceDBManager.Instance();

            foreach (var instruction in instructions.Split(';'))
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                var delay = Task.Delay(990); // wait for 990 ms instead of 1000ms so if the main method takes longer we dont loose too many
                var args = instruction.Split('|');

                short x = short.Parse(args[0]);
                short y = short.Parse(args[1]);

                System.Drawing.Color color = ColorTranslator.FromHtml(args[2]);

                var success = placeDBManager.PlacePixel(x, y, color, placeDiscordUserId);

                watch.Stop();
                if (success)
                {
                    PlaceModule.PixelPlacementTimeLastMinute.Add(watch.ElapsedMilliseconds);
                }
                else
                {
                    lock (PlaceModule.PlaceAggregateObj)
                    {
                        PlaceModule.FailedPixelPlacements++;
                    }
                }

                await delay;
            }

            return true;
        }

    }
}
