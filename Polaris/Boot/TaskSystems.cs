using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Nivera;

using Polaris.Reporting;

namespace Polaris.Boot
{
    public class TaskWatchObject : ReporterObject
    {
        public long TicksCount;
        public long TotalTicks;
        public long ErroredTicks;
    }

    public static class TaskSystems
    {
        private static Reporter reporter;

        static TaskSystems()
        {
            Log.JoinCategory("taskscheduler");

            reporter = new Reporter(120, -1, () =>
            {
                return true;
            }, () =>

            {
                return new TaskWatchObject()
                {
                    ErroredTicks = ErroredTicks,
                    TicksCount = TicksCount,
                    TotalTicks = TotalTicks
                };
            }, 
            
            x =>
            {
                bool isBehind = IsBehind();

                if (IsBehind())
                {
                    Log.Warn($"IsBehind() => true");
                }
                else
                {
                    Log.Info($"IsBehind() => false");
                }

                TaskWatchObject taskWatchObject = x as TaskWatchObject;

                long difference = taskWatchObject.TotalTicks - taskWatchObject.TicksCount;

                if (difference >= MaxTickDifference)
                {
                    Log.Error($"Task scheduler is running {difference} ticks behind! It's recommended to restart the bot. The task scheduler will restart itself if this continues!");

                    if (difference >= DifferenceToRestart)
                    {
                        List<Action> updateTasks = new List<Action>(_updateTasks);
                        List<Action> secondUpdateTasks = new List<Action>(_secondUpdateTasks);

                        Log.Error($"Task scheduler is restarting!");

                        Restart(true);

                        _updateTasks.AddRange(updateTasks);
                        _secondUpdateTasks.AddRange(secondUpdateTasks);
                    }
                }

                if (taskWatchObject.ErroredTicks >= MaxTickDifference / 2)
                {
                    Log.Error($"There are too many errored ticks! It's recommended to restart the bot. The task scheduler will restart itself if this continues!");

                    if (taskWatchObject.ErroredTicks >= DifferenceToRestart / 2)
                    {
                        List<Action> updateTasks = new List<Action>(_updateTasks);
                        List<Action> secondUpdateTasks = new List<Action>(_secondUpdateTasks);

                        Log.Error($"Task scheduler is restarting (too many errored ticks)!");

                        Restart(true);
                    }
                }
            });
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

        public const long MaxTickDifference = 100;
        public const long DifferenceToRestart = 500;

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

        public static void Restart(bool kill = false)
        {
            if (kill)
                Kill();

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
            _updateTask.Dispose();
            _updateTask = null;

            FirstExited = true;
        }

        public static void OnSecondUpdateKilled()
        {
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
