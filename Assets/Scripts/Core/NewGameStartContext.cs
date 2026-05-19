namespace CardAdventure
{
    /// <summary>
    /// 로비 씬에서 새 게임 시작 시 선택한 최소 데이터만 어드벤쳐 씬 초기화로 넘긴다.
    /// </summary>
    public static class NewGameStartContext
    {
        private static JobClassInfo selectedJob;

        public static bool HasPendingJob => selectedJob != null;

        public static void SetSelectedJob(JobClassInfo job)
        {
            selectedJob = job;
        }

        public static bool TryConsumeSelectedJob(out JobClassInfo job)
        {
            job = selectedJob;
            selectedJob = null;
            return job != null;
        }

        public static void Clear()
        {
            selectedJob = null;
        }
    }
}
