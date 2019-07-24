using System.Diagnostics;
using System.IO;

namespace KL.Command
{
    /// <summary>
    /// Command processor
    /// </summary>
    public class CommandProcessor
    {
        /// <summary>
        /// Run the command and get results
        /// </summary>
        /// <param name="commandInput"></param>
        /// <returns></returns>
        public CommandOutput Run(CommandInput commandInput)
        {
            var ret = new CommandOutput()
            {
                FinalCommand = $"{commandInput.Command} {commandInput.Arguments}"
            };

            var command = Path.Combine(commandInput.ExecuteFolder, commandInput.Command);
            var startInfo = new ProcessStartInfo(command)
            {
                CreateNoWindow = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = commandInput.ExecuteFolder,
                Arguments = commandInput.Arguments
            };

            using (var p = Process.Start(startInfo))
            {
                if (p == null)
                {
                    ret.Status = "StartFailed";
                    return ret;
                }
                ret.StdOut = p.StandardOutput.ReadToEnd();
                ret.StdErr = p.StandardError.ReadToEnd();
                ret.ExitCode = p.ExitCode;

                p.WaitForExit();
            }

            ret.Status = "Success";
            return ret;
        }
    }
}
