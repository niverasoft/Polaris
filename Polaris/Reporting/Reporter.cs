using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Polaris.Boot;

namespace Polaris.Reporting
{
    public class Reporter
    {
        private int _maxReports;
        private int _reportTime;
        private int _curTime;
        private int _reports;

        private Func<bool> _canReport;
        private Func<ReporterObject> _createReport;
        private Action<ReporterObject> _report;

        public Reporter(int reportTime, int maxReports, Func<bool> canReport, Func<ReporterObject> createReport, Action<ReporterObject> report)
        {
            _reportTime = reportTime;
            _maxReports = maxReports;   
            _canReport = canReport;
            _createReport = createReport;
            _report = report;
        }

        public void Start()
        {
            TaskSystems.RegisterSecondUpdateTasks(UpdateLoop);
        }

        private void UpdateLoop()
        {
            _curTime++;

            if (_curTime >= _reportTime)
            {
                _curTime = 0;

                if (_maxReports > 0)
                {
                    if (_reports >= _maxReports)
                    {
                        return;
                    }
                }

                if (_canReport())
                {
                    _report(_createReport());
                    _reports++;
                }
            }
        }
    }

    public class ReporterObject { }
}
