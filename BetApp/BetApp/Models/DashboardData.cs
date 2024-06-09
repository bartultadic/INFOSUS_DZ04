namespace BetApp.Models
{
    public class DashboardData
    {
        public List<BetInfo> Bets { get; set; }
        public List<TaskInfo> MyTasks { get; set; }
        public List<TaskInfo> AdminTasks { get; set; }

        public IEnumerable<BetInfo> ActiveBets => Bets.Where(instance => !instance.Ended);

        public IEnumerable<BetInfo> FinishedBets => Bets.Where(instance => instance.Ended);
    }
}
