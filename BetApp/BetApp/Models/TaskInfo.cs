namespace BetApp.Models
{
    public class TaskInfo
    {
        public string TID { get; set; }
        public string TaskKey { get; set; }
        public string TaskName { get; set; }
        public string BetUser { get; set; }
        public int BetId { get; set; }
        public string PID { get; set; }
        public DateTime StartTime { get; set; }
    }
}
