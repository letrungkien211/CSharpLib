using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
                p.WaitForExit();
                ret.StdOut = p.StandardOutput.ReadToEnd();
                ret.StdErr = p.StandardError.ReadToEnd();
                ret.ExitCode = p.ExitCode;
            }

            return ret;
        }

        /// <summary>
        /// Run the command and get results
        /// </summary>
        /// <param name="commandInput"></param>
        /// <returns></returns>
        public async Task<CommandOutput> RunAsync(CommandInput commandInput)
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
                p.WaitForExit();
                ret.StdOut = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                ret.StdErr = await p.StandardError.ReadToEndAsync().ConfigureAwait(false);
                ret.ExitCode = p.ExitCode;
            }

            return ret;
        }
    }
}
