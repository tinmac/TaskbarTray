using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskbarTray
{
    //public class TrayIconService
    //{
    //    private NotifyIcon _notifyIcon;
    //    private ContextMenuStrip _contextMenu;

    //    public void Initialize()
    //    {
    //        _contextMenu = new ContextMenuStrip();

    //        _notifyIcon = new NotifyIcon
    //        {
    //            Icon = SystemIcons.Application, // Or load custom .ico
    //            Visible = true,
    //            ContextMenuStrip = _contextMenu,
    //            Text = "Power Plan Manager"
    //        };

    //        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

    //        BuildContextMenu();
    //    }

    //    private void BuildContextMenu()
    //    {
    //        _contextMenu.Items.Clear();

    //        var plans = PowerPlanManager.LoadPowerPlans();
    //        var active = plans.FirstOrDefault(p => p.IsActive);

    //        if (active != null)
    //            _notifyIcon.Text = $"Active: {active.Name}";

    //        foreach (var plan in plans)
    //        {
    //            var planMenu = new ToolStripMenuItem
    //            {
    //                Text = plan.DisplayName,
    //                Checked = plan.IsActive
    //            };

    //            // Activate Plan
    //            var setActiveItem = new ToolStripMenuItem("Set as Active Plan");
    //            setActiveItem.Click += async (s, e) =>
    //            {
    //                await Task.Run(() => PowerPlanManager.SetActivePowerPlan(plan.Guid));
    //                BuildContextMenu();
    //            };
    //            planMenu.DropDownItems.Add(setActiveItem);
    //            planMenu.DropDownItems.Add(new ToolStripSeparator());

    //            // CPU Max % - AC
    //            var acMenu = new ToolStripMenuItem("Set CPU Max % (AC)");
    //            foreach (var val in new[] { 100, 80, 60, 40 })
    //            {
    //                var valItem = new ToolStripMenuItem($"{val}%");
    //                valItem.Click += async (s, e) =>
    //                {
    //                    await Task.Run(() => PowerPlanManager.SetCpuMaxPercentage(plan.Guid, val));
    //                    BuildContextMenu();
    //                };
    //                acMenu.DropDownItems.Add(valItem);
    //            }
    //            planMenu.DropDownItems.Add(acMenu);

    //            // CPU Max % - DC
    //            var dcMenu = new ToolStripMenuItem("Set CPU Max % (DC)");
    //            foreach (var val in new[] { 100, 80, 60, 40 })
    //            {
    //                var valItem = new ToolStripMenuItem($"{val}%");
    //                valItem.Click += async (s, e) =>
    //                {
    //                    await Task.Run(() => PowerPlanManager.SetCpuMaxPercentageDC(plan.Guid, val));
    //                    BuildContextMenu();
    //                };
    //                dcMenu.DropDownItems.Add(valItem);
    //            }
    //            planMenu.DropDownItems.Add(dcMenu);

    //            _contextMenu.Items.Add(planMenu);
    //        }

    //        _contextMenu.Items.Add(new ToolStripSeparator());
    //        _contextMenu.Items.Add("Show Window", null, (s, e) => ShowMainWindow());
    //        _contextMenu.Items.Add("Exit", null, (s, e) =>
    //        {
    //            _notifyIcon.Visible = false;
    //            _notifyIcon.Dispose();
    //            Environment.Exit(0);
    //        });
    //    }

    //    private void ShowMainWindow()
    //    {
    //        var window = App.MainWindow;
    //        window.DispatcherQueue.TryEnqueue(() =>
    //        {
    //            window.Show();
    //            window.Activate();
    //        });
    //    }
    //}
}
