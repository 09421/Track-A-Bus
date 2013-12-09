using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Configuration;
using TrackABusSim.Classes;
using MySql.Data.MySqlClient;

namespace TrackABusSim
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private SimulationRunner Runner = null;
        private bool WaitDataUpdate = false;
        public MainWindow()
        {
            InitializeComponent();
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            RefreshButton_click(null, null);
            SimuModeCombo.Items.Add("Use single bus on chosen route");
            SimuModeCombo.Items.Add("Use all busses on chosen route");
            SimuModeCombo.Items.Add("Use all busses in system");

            Runner = new SimulationRunner();
            Runner.cb.LogUpdate += HandleLogUpdate;
            Runner.cb.StartingSim += HandleSimStart;
        }


        private void StartStopRandomButton_Click(object sender, RoutedEventArgs e)
        {

            Runner.SetVal(BusNrCombo.SelectedValue, UpdateSpeedBox.Text, SimuModeCombo.SelectedIndex,
                      BusDirectionCombo.SelectedIndex, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value));
            Runner.StartStop(true);
        }

        private void StartStopFirstPoint_click(object sender, RoutedEventArgs e)
        {
            Runner.SetVal(BusNrCombo.SelectedValue, UpdateSpeedBox.Text, SimuModeCombo.SelectedIndex,
                    BusDirectionCombo.SelectedIndex, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value));
            Runner.StartStop(false);
        }


        private void ClearLogButton_click(object sender, RoutedEventArgs e)
        {
            LogText.Document.Blocks.Clear();
        }
        private void RefreshButton_click(object sender, RoutedEventArgs e)
        {
            BusNrCombo.Items.Clear();
            List<string> routes = SimulationConfig.GetBusRoutesInSystem();
            if (routes != null || routes.Count != 0)
            {
                foreach (string s in routes)
                {
                    BusNrCombo.Items.Add(s);
                }
            }
            else
            {
                LogText.AppendText("*ERROR* - No BusRoutes in database");
                SimuModeCombo.IsEnabled = false;
                return;
            }
            SimuModeCombo.IsEnabled = true;
        }

        private void AddDirection()
        {
            BusDirectionCombo.Items.Clear();
            switch (SimuModeCombo.SelectedIndex)
            {
                case 0:
                    BusDirectionCombo.Items.Add("Bus traveling in regular order");
                    BusDirectionCombo.Items.Add("Bus traveling in reverse order");
                    break;
                case 1:
                    BusDirectionCombo.Items.Add("All Busses traveling in regular order");
                    BusDirectionCombo.Items.Add("All Busses traveling in reverse order");
                    BusDirectionCombo.Items.Add("Randomize direction");
                    break;
                case 2:
                    BusDirectionCombo.Items.Add("All busses on route traveling in regular order");
                    BusDirectionCombo.Items.Add("All busses on route traveling in reverse order");
                    BusDirectionCombo.Items.Add("Randomize direction");
                    break;
                default:
                    break;
            }
            BusDirectionCombo.IsEnabled = true;
        }

        private void LogText_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogText.ScrollToEnd();
        }

        private void BusNrCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SimuModeCombo.SelectedItem != null && BusNrCombo.SelectedItem != null)
            {
                AddDirection();
            }
        }

        private void SimuModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SimuModeCombo.SelectedItem != null && BusNrCombo.SelectedItem != null)
            {
                AddDirection();
            }
         
        }

        private void MinSpeedSLider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SimulationRunner.MinValueChanged((int)e.NewValue);
            MinSpeedBlock.Text = "Minimum speed: " + ((int)e.NewValue).ToString();
        }

        private void MaxSpeedSLider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            SimulationRunner.MaxValueChanged((int)e.NewValue);
            MinSpeedSLider.Maximum = ((int)e.NewValue);
            MaxSpeedBlock.Text = "Maximum speed: " + ((int)e.NewValue).ToString();
        }

 
        private void HandleLogUpdate(object sender, SendLogMessageEventArgs e)
        {
            while(WaitDataUpdate)
            {
                Thread.Sleep(10);
            }
            WaitDataUpdate = true;
            this.Dispatcher.BeginInvoke(new Action(() => { LogText.AppendText(e.msgData); }));
            WaitDataUpdate = false;
        }

        private void HandleSimStart(object sender, StartingSimEventArgs e)
        {
            switch (e.value)
            {
                case 0:
                    StartStopButton.Background = Brushes.Green;
                    ContentBlock1.Text = "Start randomly\non route";
                    StartStopFirstPointButton.IsEnabled = true;
                    foreach (Bus bs in SimulationConfig.RunningBusses)
                    {
                        bs.log.LogUpdate -= HandleLogUpdate;
                    }
                    break;
                case 1:
                    StartStopButton.Background = Brushes.Red;
                    ContentBlock1.Text = "Stop\nSimulation";
                    StartStopFirstPointButton.IsEnabled = false;
                    foreach (Bus bs in SimulationConfig.RunningBusses)
                    {
                        bs.log.LogUpdate += HandleLogUpdate;
                    }
                    break;
                case 2:
                    StartStopFirstPointButton.Background = Brushes.Green;
                    ContentBlock2.Text = "Start at\nbeginning\nof route";
                    StartStopButton.IsEnabled = true;
                    foreach (Bus bs in SimulationConfig.RunningBusses)
                    {
                        bs.log.LogUpdate += HandleLogUpdate;
                    }
                    break;

                case 3:
                    StartStopFirstPointButton.Background = Brushes.Red;
                    ContentBlock2.Text = "Stop simulation";
                    StartStopButton.IsEnabled = false;
                    foreach (Bus bs in SimulationConfig.RunningBusses)
                    {
                        bs.log.LogUpdate += HandleLogUpdate;
                    }
                    break;
                default:
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SimulationConfig.RunningBusses == null)
                return;
            Runner.windowClosingForceStop();
        }

    }
}
