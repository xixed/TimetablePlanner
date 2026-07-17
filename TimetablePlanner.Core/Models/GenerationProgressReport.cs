namespace TimetablePlanner.Core.Models
{
    public class GenerationProgressReport
    {
        /// <summary>
        /// Overall progress in range [0..1]
        /// </summary>
        public double Overall { get; set; }

        /// <summary>
        /// Stage-specific progress in range [0..1]
        /// </summary>
        public double StageProgress { get; set; }

        /// <summary>
        /// Current stage name (e.g., "Import", "Greedy", "MAXSAT")
        /// </summary>
        public string? Stage { get; set; }
    }
}
