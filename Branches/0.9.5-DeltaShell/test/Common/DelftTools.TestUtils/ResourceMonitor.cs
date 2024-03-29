﻿using System.Windows.Forms;

namespace DelftTools.TestUtils
{
    using System;
    using System.Diagnostics;

    public partial class ResourceMonitor : Form
    {
        private Timer refreshTimer;
        
        public ResourceMonitor()
        {
            InitializeComponent();

            refreshTimer = new Timer { Interval = 200 };
            refreshTimer.Tick += refreshTimer_Tick;

            Utils.Diagnostics.ResourceMonitor.ResourceAllocated += ResourceMonitor_ResourceAllocated;
            Utils.Diagnostics.ResourceMonitor.ResourceDeallocated += ResourceMonitor_ResourceDeallocated;
        }

        void refreshTimer_Tick(object sender, EventArgs e)
        {
            SuspendLayout();
            labelAllocatedBitmaps.Text = allocatedResourcesCount.ToString();
            ResumeLayout();

            textBoxStackTrace.Text = lastStackTrace;
            refreshTimer.Stop();
        }

        private int allocatedResourcesCount;

        private string lastStackTrace;

        private bool paused;

        void ResourceMonitor_ResourceAllocated(object arg1, object arg2)
        {
            while (paused)
            {
                Application.DoEvents();
            }

            allocatedResourcesCount++;
            UpdateStackTrace();
            if(!refreshTimer.Enabled)
            {
                refreshTimer.Start();
            }
        }

        private void ResourceMonitor_ResourceDeallocated(object arg1, object arg2)
        {
            while (paused)
            {
                Application.DoEvents();
            }

            allocatedResourcesCount--;
            UpdateStackTrace();
            if (!refreshTimer.Enabled)
            {
                refreshTimer.Start();
            }
        }

        private void UpdateStackTrace()
        {
            lastStackTrace = new StackTrace(true).ToString();
        }

        private void buttonPauseContinue_Click(object sender, EventArgs e)
        {
            paused = !paused;

            buttonPauseContinue.Text = paused ? "Continue" : "Pause";
        }
    }
}
