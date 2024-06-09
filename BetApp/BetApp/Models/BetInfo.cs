namespace BetApp.Models
{
    public class BetInfo
    {
        public string User { get; set; }
        public int BetId { get; set; }
        public string PID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool Ended { get; set; }
        public long BetAmount { get; set; }
        public bool CanApplyForReview { get; set; }
        public string Admin { get; set; }
        public bool betResult { get; set; }
        public string Status { get; set; }

    }
}
