using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace pi_intercom.State
{
    public class PlaybackState : IPlaybackState
    {
        public bool InUse { get; set; }
        public string CurrentUser { get; set; }
    }
}
