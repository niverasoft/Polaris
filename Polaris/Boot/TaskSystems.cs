using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Nivera;

namespace Polaris.Boot
{
    public static class TaskSystems
    {
        static TaskSystems()
        {
            Log.JoinCategory("taskscheduler");
        }

        private static bool FirstExited;
        private static bool SecondExited;

        private static List<Action> _updateTasks = new List<Action>();
        private static List<Action> _secondUpdateTasks = new List<Action>();

        private static Task _updateTask;
        private static Task _secondUpdateTask;

        public static bool HasExited;
        public static bool IsPaused;
        public static bool ShouldKill;

        public static long TicksCount;
        public static long TotalTicks;
        public static long ErroredTicks;

        public const long MaxTickDifference = 5000;

        public static bool IsBehind()
        {
            return TicksCount - ErroredTicks > MaxTickDifference;
        }

        public static void Kill()
        {
            ShouldKill = true;

            while (!FirstExited && !SecondExited)
                continue;

            HasExited = true;
        }

        public static void Restart()
        {
            IsPaused = false;
            ShouldKill = false;
            HasExited = false;

            StartUpdateTask();
            StartSecondUpdateTask();
        }

        public static void RegisterUpdateTask(Action action)
        {
            _updateTasks.Add(action);
        }

        public static void RegisterSecondUpdateTasks(Action action)
        {
            _secondUpdateTasks.Add(action);
        }

        public static void OnUpdateKilled()
        {
            Log.Verbose("Performing Update Cleanup");

            _updateTask.Dispose();
            _updateTask = null;

            FirstExited = true;
        }

        public static void OnSecondUpdateKilled()
        {
            Log.Verbose("Performing Second Update Cleanup");

            _secondUpdateTask.Dispose();
            _secondUpdateTask = null;

            SecondExited = true;
        }

        public static void StartUpdateTask()
        {
            _updateTask = Task.Run(async () =>
            {
                while (!ShouldKill)
                {
                    if (IsPaused)
                        continue;
                    else
                    {
                        await Task.Delay(500);

                        CallUpdateTask();
                    }
                }

                Log.Verbose("Killed Update Task");

                OnUpdateKilled();
            });
        }

        public static void StartSecondUpdateTask()
        {
            _secondUpdateTask = Task.Run(async () =>
            {
                while (!ShouldKill)
                {
                    TotalTicks++;

                    if (IsPaused)
                        continue;
                    else
                    {
                        await Task.Delay(1000);

                        CallSecondUpdateTask();
                    }
                }

                Log.Verbose("Killed Second Update Task");

                OnSecondUpdateKilled();
            });
        }

        private static void CallUpdateTask()
        {
            foreach (Action action in _updateTasks)
            {
                try
                {
                    TotalTicks++;

                    action();

                    TicksCount++;
                }
                catch (Exception ex)
                {
                    ErroredTicks++;

                    Log.Fatal($"{action.Method.DeclaringType.FullName.ToLower()}.{action.Method.Name.ToLower()} caused an exception: {ex.Message}");
                }
            }
        }

        private static void CallSecondUpdateTask()
        {
            foreach (Action action in _secondUpdateTasks)
            {
                try
                {
                    action();

                    TicksCount++;
                }
                catch (Exception ex)
                {
                    ErroredTicks++;

                    Log.Fatal($"{action.Method.DeclaringType.FullName.ToLower()}.{action.Method.Name.ToLower()} caused an exception: {ex.Message}");
                }
            }
        }
    }
}
