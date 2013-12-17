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

        /// <summary>
        /// Contructor for the window. Changes floating point to "." instead of ",", sets the simulation modes,
        /// creates the runner and adds the custom event handlers.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            
            //Calls this method instead of setting possible busroutes directly.
            RefreshButton_click(null, null);
            SimuModeCombo.Items.Add("Use single bus on chosen route");
            SimuModeCombo.Items.Add("Use all busses on chosen route");
            SimuModeCombo.Items.Add("Use all busses in system");

            Runner = new SimulationRunner();
            Runner.cb.LogUpdate += HandleLogUpdate;
            Runner.cb.StartingSim += HandleSimStart;
        }

        /// <summary>
        /// Handler for the click of the start button, that places the busses on a random spot on the route.
        /// </summary>
        private void StartStopRandomButton_Click(object sender, RoutedEventArgs e)
        {
            //Setting simulation configurations.
            Runner.SetVal(BusNrCombo.SelectedValue, UpdateSpeedBox.Text, SimuModeCombo.SelectedIndex,
                      BusDirectionCombo.SelectedIndex, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value));
            //Starting the simualtion
            Runner.StartStop(true);
        }

        /// <summary>
        /// Handler for the click of the start button, that places the busses on at the start of the route
        /// </summary>
        private void StartStopFirstPoint_click(object sender, RoutedEventArgs e)
        {
            Runner.SetVal(BusNrCombo.SelectedValue, UpdateSpeedBox.Text, SimuModeCombo.SelectedIndex,
                    BusDirectionCombo.SelectedIndex, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value));
            Runner.StartStop(false);
        }

        /// <summary>
        /// Handles the click event of the clear log button. This will completly delete all the entries in the logging window.
        ///</summary>
        private void ClearLogButton_click(object sender, RoutedEventArgs e)
        {
            LogText.Document.Blocks.Clear();
        }

        /// <summary>
        /// Sets all the possible routes in the ComboBox.
        /// </summary>
        private void RefreshButton_click(object sender, RoutedEventArgs e)
        {
            //Remove current busroutes from the box.
            BusNrCombo.Items.Clear();
            //Get possible routes in system
            List<string> routes = SimulationConfig.GetBusRoutesInSystem();
            //Add routes to the box, if the list is not null and contains busroutes.
            //Otherwise log the error, and disable the box.
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

        /// <summary>
        /// Add direction depending on which simulation mode is chosen.
        /// </summary>
        private void AddDirection()
        {
            BusDirectionCombo.Items.Clear();
            switch (SimuModeCombo.SelectedIndex)
            {
                //Single bus
                case 0:
                    BusDirectionCombo.Items.Add("Bus traveling in regular order");
                    BusDirectionCombo.Items.Add("Bus traveling in reverse order");
                    break;
                //All busses on route.
                case 1:
                    BusDirectionCombo.Items.Add("All Busses traveling in regular order");
                    BusDirectionCombo.Items.Add("All Busses traveling in reverse order");
                    BusDirectionCombo.Items.Add("Randomize direction");
                    break;
                //All busses in the system.
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

        /// <summary>
        /// Scrolls to the bottom of the logging window when new messsage is added.
        /// </summary>
        private void LogText_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogText.ScrollToEnd();
        }

        /// <summary>
        /// Enables the directions box, if simulation mode and busroute is chosen.
        /// </summary>
        private void BusNrCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SimuModeCombo.SelectedItem != null && BusNrCombo.SelectedItem != null)
            {
                AddDirection();
            }
        }

        /// <summary>
        /// Enables the directions box, if simulation mode and busroute is chosen.
        /// </summary>
        private void SimuModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SimuModeCombo.SelectedItem != null && BusNrCombo.SelectedItem != null)
            {
                AddDirection();
            }
         
        }
        /// <summary>
        /// Updates all busses with the new minimum speed.
        /// </summary>
        private void MinSpeedSLider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SimulationRunner.MinValueChanged((int)e.NewValue);
            MinSpeedBlock.Text = "Minimum speed: " + ((int)e.NewValue).ToString();
        }

        /// <summary>
        /// Updates all busses with the new maximum speed, 
        /// and changes the maximum value of the minimum slider to be the new value
        /// so a minimum speed cant be greater than the maximum.
        /// </summary>
        private void MaxSpeedSLider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            SimulationRunner.MaxValueChanged((int)e.NewValue);
            MinSpeedSLider.Maximum = ((int)e.NewValue);
            MaxSpeedBlock.Text = "Maximum speed: " + ((int)e.NewValue).ToString();
        }

        /// <summary>
        /// Custom event. Handles the log messages the logic layer sends.
        /// This is done in the main thread, so a binary semaphore handles the waiting of multiple thread acces.
        /// </summary>
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

        /// <summary>
        /// Custom event. Handles which state the simulator should be in.
        /// </summary>
        private void HandleSimStart(object sender, StartingSimEventArgs e)
        {
            switch (e.value)
            {
                //A value of zero means the simulator should be put in a stopped state,
                //The previous state was that of started randomly.
                case 0:
                    StartStopButton.Background = Brushes.Green;
                    ContentBlock1.Text = "Start randomly\non route";
                    StartStopFirstPointButton.IsEnabled = true;
                    foreach (Bus bs in SimulationConfig.RunningBusses)
                    {
                        bs.log.LogUpdate -= HandleLogUpdate;
                    }
                    WaitDataUpdate = false;
                    break;
                //A value of 1 means the simulator should be put in a started state, with random placement of busses.
                case 1:
                    StartStopButton.Background = Brushes.Red;
                    ContentBlock1.Text = "Stop\nSimulation";
                    StartStopFirstPointButton.IsEnabled = false;
                    foreach (Bus bs in SimulationConfig.RunningBusses)
                    {
                        bs.log.LogUpdate += HandleLogUpdate;
                    }
                    break;
                //A value of 2 means the simulator should be put in a stopped state,
                //The previous state was that of started at the beginning of the route.
                case 2:
                    StartStopFirstPointButton.Background = Brushes.Green;
                    ContentBlock2.Text = "Start at\nbeginning\nof route";
                    StartStopButton.IsEnabled = true;
                    foreach (Bus bs in SimulationConfig.RunningBusses)
                    {
                        bs.log.LogUpdate += HandleLogUpdate;
                    }
                    WaitDataUpdate = false;
                    break;
                //A value of 3 means the simulator should be put in a started state,
                //where the busses are placed at the beginning of the route.
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

        /// <summary>
        /// Simulator is closing. Stops all running busses.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SimulationConfig.RunningBusses == null)
                return;
            Runner.windowClosingForceStop();
        }

    }
}
