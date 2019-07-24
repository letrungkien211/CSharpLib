namespace KL.Command
{
    /// <summary>
    /// Command input
    /// </summary>
    public class CommandInput
    {
        /// <summary>
        /// Command. Eg: convert.exe, cmd.exe
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Arguments. Eg: abc.jpg def.jpg
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Execute Folder
        /// </summary>
        public string ExecuteFolder { get; set; }
    }
}
