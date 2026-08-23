using System;

namespace DualBootSwitcher
{
    internal static class BootNameValidator
    {
        private const int MaximumLength = 80;

        public static bool TryValidate(string value, out string error)
        {
            string candidate = value == null ? string.Empty : value.Trim();
            if (candidate.Length == 0)
            {
                error = "启动项名称不能为空。";
                return false;
            }

            if (candidate.Length > MaximumLength)
            {
                error = "启动项名称不能超过 80 个字符。";
                return false;
            }

            foreach (char character in candidate)
            {
                if (char.IsControl(character) || character == '"')
                {
                    error = "启动项名称不能包含换行、控制字符或双引号。";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
