using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal static class UiMotion
    {
        private const uint GetClientAreaAnimation = 0x1042;
        private static readonly List<ActiveAnimation> Animations = new List<ActiveAnimation>();
        private static Timer timer;
        private static int nextToken;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(
            uint action,
            uint parameter,
            [MarshalAs(UnmanagedType.Bool)] ref bool value,
            uint updateFlags);

        public static bool IsEnabled
        {
            get
            {
                bool enabled = true;
                try
                {
                    return SystemParametersInfo(GetClientAreaAnimation, 0, ref enabled, 0) && enabled;
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
                catch (EntryPointNotFoundException)
                {
                    return false;
                }
            }
        }

        public static int Start(
            Action<float> update,
            int durationMilliseconds,
            Action completed,
            Func<float, float> easing)
        {
            if (update == null)
            {
                throw new ArgumentNullException("update");
            }

            if (!IsEnabled || durationMilliseconds <= 0)
            {
                update(1F);
                if (completed != null)
                {
                    completed();
                }

                return 0;
            }

            int token = GetNextToken();
            Animations.Add(new ActiveAnimation
            {
                Token = token,
                Update = update,
                Completed = completed,
                Easing = easing ?? EaseOutQuart,
                DurationMilliseconds = Math.Max(1, durationMilliseconds),
                StartedAt = DateTime.UtcNow
            });
            EnsureTimerRunning();
            return token;
        }

        public static int Start(Action<float> update, int durationMilliseconds)
        {
            return Start(update, durationMilliseconds, null, EaseOutQuart);
        }

        public static void Stop(int token)
        {
            if (token == 0)
            {
                return;
            }

            for (int index = Animations.Count - 1; index >= 0; index--)
            {
                if (Animations[index].Token == token)
                {
                    Animations.RemoveAt(index);
                }
            }

            StopTimerWhenIdle();
        }

        public static void StopAll()
        {
            Animations.Clear();
            StopTimerWhenIdle();
        }

        public static float EaseOutQuart(float value)
        {
            float clamped = Clamp(value);
            float inverse = 1F - clamped;
            return 1F - (inverse * inverse * inverse * inverse);
        }

        public static float EaseOutCubic(float value)
        {
            float clamped = Clamp(value);
            float inverse = 1F - clamped;
            return 1F - (inverse * inverse * inverse);
        }

        public static float Lerp(float from, float to, float progress)
        {
            return from + ((to - from) * Clamp(progress));
        }

        public static Color Blend(Color from, Color to, float progress)
        {
            float clamped = Clamp(progress);
            return Color.FromArgb(
                BlendChannel(from.A, to.A, clamped),
                BlendChannel(from.R, to.R, clamped),
                BlendChannel(from.G, to.G, clamped),
                BlendChannel(from.B, to.B, clamped));
        }

        private static int GetNextToken()
        {
            unchecked
            {
                nextToken++;
                if (nextToken <= 0)
                {
                    nextToken = 1;
                }
            }

            return nextToken;
        }

        private static void EnsureTimerRunning()
        {
            if (timer != null)
            {
                return;
            }

            timer = new Timer { Interval = UiTheme.MotionFrameInterval };
            timer.Tick += OnTick;
            timer.Start();
        }

        private static void OnTick(object sender, EventArgs eventArgs)
        {
            DateTime now = DateTime.UtcNow;
            var completed = new List<ActiveAnimation>();
            ActiveAnimation[] snapshot = Animations.ToArray();
            foreach (ActiveAnimation animation in snapshot)
            {
                if (!Animations.Contains(animation))
                {
                    continue;
                }

                float linear = Math.Min(
                    1F,
                    (float)((now - animation.StartedAt).TotalMilliseconds /
                        animation.DurationMilliseconds));
                animation.Update(animation.Easing(linear));
                if (!Animations.Contains(animation))
                {
                    continue;
                }
                if (linear >= 1F)
                {
                    Animations.Remove(animation);
                    completed.Add(animation);
                }
            }

            StopTimerWhenIdle();
            foreach (ActiveAnimation animation in completed)
            {
                if (animation.Completed != null)
                {
                    animation.Completed();
                }
            }
        }

        private static void StopTimerWhenIdle()
        {
            if (Animations.Count != 0 || timer == null)
            {
                return;
            }

            timer.Stop();
            timer.Dispose();
            timer = null;
        }

        private static float Clamp(float value)
        {
            return Math.Max(0F, Math.Min(1F, value));
        }

        private static int BlendChannel(int from, int to, float progress)
        {
            return (int)Math.Round(from + ((to - from) * progress));
        }

        private sealed class ActiveAnimation
        {
            public int Token;
            public Action<float> Update;
            public Action Completed;
            public Func<float, float> Easing;
            public int DurationMilliseconds;
            public DateTime StartedAt;
        }
    }
}
