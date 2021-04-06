using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Enums
{
    // In sync with the unity project (manually)
    public enum MessageEnum
    {
        FullImage_Request = 1,
        FullImage_Response = 2,
        LivePixel = 3,
        TotalPixelCount_Request = 4,
        TotalPixelCount_Response = 5,
        TotalChunksAvailable_Request = 6,
        TotalChunksAvailable_Response = 7,
        GetChunk_Request = 8,
        GetChunk_Response = 9,
        GetUsers_Request = 10,
        GetUsers_Response = 11,
        GetUserProfileImage_Request = 12,
        GetUserProfileImage_Response = 13
    }
}
