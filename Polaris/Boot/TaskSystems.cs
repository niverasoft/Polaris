using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using NiveraLib;
using NiveraLib.Timers;
using NiveraLib.Logging;

namespace Polaris.Boot
{
    public static class TaskSystems
    {
        private static Timer reporter;
        private static LogId logId = new LogId("core / taskScheduler", 102);

        static TaskSystems()
        {
            reporter = new Timer("TaskSchedulerTickChecker", false, 120000, (x, y) =>
            {
                long difference = TotalTicks - ErroredTicks;

                if (difference >= MaxTickDifference)
                {
                    Log.SendError($"Task scheduler is running {difference} ticks behind! It's recommended to restart the bot. The task scheduler will restart itself if this continues!", logId);

                    if (difference >= DifferenceToRestart)
                    {
                        List<Action> updateTasks = new List<Action>(_updateTasks);
                        List<Action> secondUpdateTasks = new List<Action>(_secondUpdateTasks);

                        Log.SendError($"Task scheduler is restarting!");

                        Restart(true);

                        _updateTasks.AddRange(updateTasks);
                        _secondUpdateTasks.AddRange(secondUpdateTasks);
                    }
                }

                if (ErroredTicks >= MaxTickDifference / 2)
                {
                    Log.SendError($"There are too many errored ticks! It's recommended to restart the bot. The task scheduler will restart itself if this continues!", logId);

                    if (ErroredTicks >= DifferenceToRestart / 2)
                    {
                        List<Action> updateTasks = new List<Action>(_updateTasks);
                        List<Action> secondUpdateTasks = new List<Action>(_secondUpdateTasks);

                        Log.SendError($"Task scheduler is restarting (too many errored ticks)!");

                        Restart(true);

                        _updateTasks.AddRange(updateTasks);
                        _secondUpdateTasks.AddRange(secondUpdateTasks);
                    }
                }
            });

            reporter.Start();
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

        public static long TotalTicks;
        public static long ErroredTicks;

        public const long MaxTickDifference = 5;
        public const long DifferenceToRestart = 15;

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
            TotalTicks = 0;
            ErroredTicks = 0;

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
                }
                catch (Exception ex)
                {
                    ErroredTicks++;

                    Log.SendFatal($"{action.Method.DeclaringType.FullName.ToLower()}.{action.Method.Name.ToLower()} caused an exception: {ex.Message}", logId);
                }
            }
        }

        private static void CallSecondUpdateTask()
        {
            foreach (Action action in _secondUpdateTasks)
            {
                try
                {
                    TotalTicks++;

                    action();
                }
                catch (Exception ex)
                {
                    ErroredTicks++;

                    Log.SendFatal($"{action.Method.DeclaringType.FullName.ToLower()}.{action.Method.Name.ToLower()} caused an exception: {ex.Message}", logId);
                }
            }
        }
    }
}
