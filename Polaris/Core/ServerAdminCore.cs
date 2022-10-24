using Polaris.Boot;
using Polaris.Entities;

using System;
using System.Collections.Generic;

namespace Polaris.Core
{
    public class ServerAdminCore
    {
        private PunishmentUpdate _punishmentUpdate;
    }

    public class PunishmentUpdate
    {
        private List<PunishmentLog> _punishments;

        public void Start()
        {
            ValidateAll();

            TaskSystems.RegisterSecondUpdateTasks(Update);
        }

        public void Update()
        {
            foreach (var log in _punishments)
            {
                if (log.IsPermanent || log.HasExpired)
                    continue;

                log.RemaningSeconds--;
                log.LastUpdateAt = DateTime.Now;

                if (log.RemaningSeconds <= 0)
                {
                    log.RemaningSeconds = 0;
                    log.HasExpired = true;

                    HandleExpiredLog(log);
                }
            }
        }

        public void HandleExpiredLog(PunishmentLog log)
        {

        }

        private void ValidateAll()
        {
            foreach (var log in _punishments)
            {
                if (log.IsPermanent || log.HasExpired)
                    continue;

                int seconds = (log.ExpiresAt - log.LastUpdateAt).Seconds;

                if (seconds != log.RemaningSeconds)
                    log.RemaningSeconds = seconds;

                if (log.RemaningSeconds <= 0)
                    log.HasExpired = true;
            }
        }
    }
}