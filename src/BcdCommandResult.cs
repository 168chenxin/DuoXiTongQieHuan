namespace DualBootSwitcher
{
    internal sealed class BcdCommandResult
    {
        public BcdCommandResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? string.Empty;
            StandardError = standardError ?? string.Empty;
        }

        public int ExitCode { get; private set; }

        public string StandardOutput { get; private set; }

        public string StandardError { get; private set; }

        public bool IsSuccess
        {
            get { return ExitCode == 0; }
        }

        public string CombinedOutput
        {
            get
            {
                if (string.IsNullOrWhiteSpace(StandardOutput))
                {
                    return StandardError;
                }

                if (string.IsNullOrWhiteSpace(StandardError))
                {
                    return StandardOutput;
                }

                return StandardOutput.TrimEnd() + "\r\n" + StandardError.TrimStart();
            }
        }

        public string ErrorDetails
        {
            get
            {
                return CombinedOutput.Trim();
            }
        }
    }
}
