using System;
using System.Threading;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using WPFMonitorProgress.Controls;
using WPFMonitorProgress.Views;

namespace WPFMonitorProgress.Models
{
    class ChangeParameter
    {
        public ChangeParameter(Element wallElement)
        {
            CurrentWall = wallElement as Wall;
        }

        Wall CurrentWall { get; }
        ProgressMonitorControl CurrentControl { get; set; }
        ProgressMonitorView CurrentUI { get; set; }
        bool Cancel { get; set; }

        ExternalEvent ExternalEvent { get; set; }

        public void ProgressModal()
        {
            if (CurrentWall == null)
                throw new Exception("Selected Element is not a wall");

            CurrentControl = new ProgressMonitorControl();
            CurrentControl.MaxValue = 100;
            CurrentUI = new ProgressMonitorView();
            CurrentUI.DataContext = CurrentControl;
            CurrentUI.Closed += CurrentUI_Closed;
            CurrentUI.ContentRendered += FireUPModal;

            CurrentUI.ShowDialog();
        }

        void FireUPModal(object sender, EventArgs e)
        {
            CurrentUI.ContentRendered -= FireUPModal;
            Parameter parameter = CurrentWall.get_Parameter(BuiltInParameter.DOOR_NUMBER);
            if (parameter.IsReadOnly)
            {
                CloseWindow();
                throw new Exception("Mark parameter is read only");
            }

            using (Transaction t = new Transaction(CurrentWall.Document, "Process"))
            {
                t.Start();
                for (CurrentControl.CurrentValue = 0;
                    CurrentControl.CurrentValue <= CurrentControl.MaxValue;
                    ++CurrentControl.CurrentValue)
                {
                    if (Cancel)
                        break;

                    Thread.Sleep(50);

                    try
                    {
                        parameter.Set(CurrentControl.CurrentValue.ToString());
                    }
                    catch
                    {
                        CloseWindow();
                        throw new Exception("Error trying to set Mark paramter");
                    }

                    CurrentControl.CurrentContext = string.Format("progress {0} of {1} done",
                        CurrentControl.CurrentValue, CurrentControl.MaxValue);
                    CurrentUI.Dispatcher.Invoke(new ProgressBarDelegate(CurrentControl.NotifyUI),
                        DispatcherPriority.Background);
                }

                t.Commit();
            }

            CloseWindow();
        }

        void CloseWindow()
        {
            CurrentUI.Closed -= CurrentUI_Closed;
            CurrentUI.Close();
        }

        void CurrentUI_Closed(object sender, EventArgs e)
        {
            Cancel = true;
        }

        delegate void ProgressBarDelegate();
    }
}