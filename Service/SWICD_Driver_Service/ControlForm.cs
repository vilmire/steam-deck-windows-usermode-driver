﻿using SWICD_Lib.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SWICD_Driver_Service
{
    public partial class ControlForm : Form
    {
        Process DriverProcess;
        string InstallationDirectory;
        List<string> DriverLog = new List<string>();
        Thread _driverLogWorker;
        Thread _driverManagementWorker;
        bool _running = true;
        bool DriverShouldRun = true;
        EventLog log = new EventLog()
        {
            Source = "SWICD_Driver",
        };
        bool IsDriverRunning => !DriverProcess?.HasExited ?? false;

        public ControlForm()
        {
            InitializeComponent();
            InstallationDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _driverLogWorker = new Thread(new ThreadStart(DriverLogWorker));
            _driverLogWorker.IsBackground = true;
            _driverLogWorker.Start();
            _driverManagementWorker = new Thread(new ThreadStart(DriverManagementWorker));
            _driverManagementWorker.IsBackground = true;
            _driverManagementWorker.Start();
            Application.ApplicationExit += Application_ApplicationExit;
            Hide();
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            StopDriver();
        }

        void StartDriver()
        {
            var filename = Path.Combine(InstallationDirectory, "SWICD_Driver.exe");
            var logname = Path.Combine(InstallationDirectory, "SWICD_Driver.log");
            DriverProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"SWICD_Driver.exe\"",
                    WorkingDirectory = InstallationDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            DriverProcess.Start();
            DriverShouldRun = true;
        }

        void DriverLogWorker()
        {
            while (_running)
            {
                try
                {
                    if (DriverProcess != null)
                    {
                        try
                        {
                            var stdline = DriverProcess.StandardOutput.ReadLine();
                            if (stdline != null)
                            {
                                DriverLog.Add(stdline);
                                log.WriteEntry(stdline, EventLogEntryType.Information);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.WriteEntry(ex.ToString(), EventLogEntryType.Error);
                        }

                        try
                        {
                            var errline = DriverProcess.StandardError.ReadLine();
                            if (errline != null)
                            {
                                log.WriteEntry(errline, EventLogEntryType.Error);
                                DriverLog.Add(errline);
                            }

                        }
                        catch (Exception ex)
                        {
                            log.WriteEntry(ex.ToString(), EventLogEntryType.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.WriteEntry(ex.ToString(), EventLogEntryType.Error);

                }
                Thread.Sleep(100);
            }

        }
        void StopDriver()
        {
            if (DriverProcess != null && !DriverProcess.HasExited)
            {
                DriverProcess.Kill();
            }
            DriverShouldRun = false;
        }

        private void DriverManagementWorker()
        {
            while (_running)
            {
                try
                {
                    if (!IsDriverRunning && DriverShouldRun)
                        StartDriver();
                }
                catch (Exception ex)
                {
                    log.WriteEntry(ex.ToString(), EventLogEntryType.Error);
                }
                Thread.Sleep(1000);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
        }

        private void ControlForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void timerGuiUpdate_Tick(object sender, EventArgs e)
        {
            lbDriverLog.Items.Clear();
            List<string> log = DriverLog;
            foreach (var line in log)
                lbDriverLog.Items.Add(line);
            lbDriverLog.SelectedIndex = lbDriverLog.Items.Count - 1;

            lblDriverStatus.Text = IsDriverRunning ? "Running" : "Stopped";
            tsmiStartStopDriver.Text = IsDriverRunning ? "Stop Driver" : "Start Driver";
            notificationIcon.Icon = IsDriverRunning ? Resources.app_icon_on : Resources.app_icon_off;
            lblDriverStatus.BackColor = IsDriverRunning ? Color.ForestGreen : Color.DarkRed;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            if (IsDriverRunning)
                StopDriver();
            else
                StartDriver();
        }

        private void ControlForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _running = false;
            if (IsDriverRunning)
                StopDriver();
        }

        private void tsmiExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tsmiShow_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void tsmiStartStopDriver_Click(object sender, EventArgs e)
        {
            if (IsDriverRunning)
                StopDriver();
            else
                StartDriver();
        }
    }
}