namespace pi_intercom.State
{
    public interface IPlaybackState
    {
        bool InUse { get; set; }
        public string CurrentUser { get; set; }
    }
}