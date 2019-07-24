namespace KL.Command
{
    /// <summary>
    /// Command output
    /// </summary>
    public class CommandOutput
    {
        /// <summary>
        /// Output status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Stdout
        /// </summary>
        public string StdOut { get; set; }

        /// <summary>
        /// Stderr
        /// </summary>
        public string StdErr { get; set; }

        /// <summary>
        /// Exit code
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Final command which is executed
        /// </summary>
        public string FinalCommand { get; set; }
    }

    /// <summary>
    /// Output status
    /// </summary>
    public enum OutputStatus
    {
        /// <summary>
        /// Unknown. Not set.
        /// </summary>
        Unknown,

        /// <summary>
        /// Success
        /// </summary>
        Success,

        /// <summary>
        /// Start failed
        /// </summary>
        StartFailed
    }
}
