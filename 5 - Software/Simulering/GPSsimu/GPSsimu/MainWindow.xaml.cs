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
        private bool isRunning = false;
        private List<Bus> busList = new List<Bus>();
        private List<BusRoute> simulatingRoute = new List<BusRoute>();
        private List<BusRoute> allRoutes = new List<BusRoute>();
        private Random r = new Random();
        private Logger l = new Logger();
        public MainWindow()
        {
            InitializeComponent();
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            GetBusRoutesInSystem();
        }
        private void StartStopButton_Click_1(object sender, RoutedEventArgs e)
        {

            if (!isRunning)
            {
                int Check = CheckVal();
                if (Check == -1)
                {
                    return;
                }
                Truncate();
                simulatingRoute = CreateSimuBusRoutes();
                allRoutes = CreateAllBusRoutes();
                CreateBusses();
                StartStopButton.Background = Brushes.Red;
                StartStopButton.Content = "Stop Simulation";
                foreach (Bus bs in busList)
                {
                    bs.log.LogUpdate += HandleDataUpdate; 
                    bs.startSim();
                }
                isRunning = true;
            }
            else
            {
                StartStopButton.Background = Brushes.Green;
                StartStopButton.Content = "Start Simulation";
                foreach (Bus bs in busList)
                {
                    bs.stopSim();
                }
                isRunning = false;
            }
        }

        private int CheckVal()
        {

            int uSpeed;

            if (!BusNrCombo.IsEnabled || BusNrCombo.SelectedItem == null)
            {
                LogText.AppendText("*ERROR* - Busroute not chosen, or no busroutes in system\n");
                return -1;
            }

            if (SimuModeCombo.SelectedItem == null)
            {
                LogText.AppendText("*ERROR* - No simulation mode chosen\n");
                return -1;
            }

            if (!int.TryParse(UpdateSpeedBox.Text, out uSpeed))
            {
                LogText.AppendText("*ERROR* - Please only use whole numbers as update speed\n");
                return -1;
            }
            if (uSpeed <= 0)
            {
                LogText.AppendText("*ERROR* - Update speed cannot be lower or equal to zero\n");
                return -1;
            }
            LogText.AppendText("Configuration OK" + Environment.NewLine);
            return 0;
        }

        private List<BusRoute> CreateAllBusRoutes()
        {
            
            List<BusRoute> Routes = new List<BusRoute>();
            
            string query = "select distinct BusRoute.ID, BusRoute.RouteNumber from BusRoute" +
                           " inner join Bus on Bus.fk_BusRoute = BusRoute.ID " +
                            "inner join BusRoute_RoutePoint on BusRoute.ID = BusRoute_RoutePoint.fk_BusRoute";
            List<string> AllRoutesNonCreated = DatabaseAcces.Query(query, new List<string>() { "ID", "Routenumber" });
            for (int i = 0; i < AllRoutesNonCreated.Count; i = i + 2)
                Routes.Add(new BusRoute(AllRoutesNonCreated[i], AllRoutesNonCreated[i + 1]));
            
            return Routes;
        }

        private List<BusRoute> CreateSimuBusRoutes()
        {

            List<BusRoute> Routes = new List<BusRoute>();
            string query = "select ID from BusRoute where RouteNumber = '" + BusNrCombo.SelectedItem.ToString() + "'";
            List<string> SimuRoutesNonCreated = DatabaseAcces.Query(query, new List<string>() { "ID"});
            for (int i = 0; i < SimuRoutesNonCreated.Count; i++)
            {
                Routes.Add(new BusRoute(SimuRoutesNonCreated[i], BusNrCombo.SelectedItem.ToString()));
            }
            return Routes;
        }

        private void CreateBusses()
        {
            bool desc = false;
            List<Bus> bs = new List<Bus>();
            List<int> fkBusRouteList = new List<int>();
            List<BusRoute> routesToPass = new List<BusRoute>();
            List<Tuple<string, string>> routeList = new List<Tuple<string, string>>();

            string singleBusQuery = "Select ID from Bus where ";
            string allBusOnRouteQuery = "Select ID from Bus where ";
            string AllBusQuery = "select distinct Bus.ID, Bus.fk_BusRoute from Bus "
                                  +" inner join BusRoute on Bus.fk_BusRoute = BusRoute.ID"
                                  +" inner join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_BusRoute = BusRoute.ID";
            foreach (BusRoute br in simulatingRoute)
            {
                singleBusQuery += "fk_busRoute = " + br.id + " or ";
                allBusOnRouteQuery += "fk_busRoute = " + br.id + " or ";
            }

            singleBusQuery = singleBusQuery.TrimEnd(" or ".ToCharArray()) + " limit 1";
            allBusOnRouteQuery = allBusOnRouteQuery.TrimEnd(" or ".ToCharArray());
            switch (SimuModeCombo.SelectedIndex)
            {
                case 0:
                    int id = int.Parse(DatabaseAcces.Query(singleBusQuery, new List<string>() { "ID" })[0].ToString());
                    routesToPass = simulatingRoute.Clone<BusRoute>();
                    switch (BusDirectionCombo.SelectedIndex)
                    {
                        case 0:
                           
                            bs.Add(new Bus(id, routesToPass, r, int.Parse(UpdateSpeedBox.Text), false, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value)));
                            break;
                        case 1:
                            bs.Add(new Bus(id, routesToPass, r, int.Parse(UpdateSpeedBox.Text), true, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value)));
                            break;
                        default:
                            break;
                    }
                    break;
                case 1:
                    List<string> IDList = DatabaseAcces.Query(allBusOnRouteQuery, new List<string>() { "ID" });
             
                    switch (BusDirectionCombo.SelectedIndex)
                    {

                        case 0:
                            for (int i = 0; i < IDList.Count; i++)
                            {
                                routesToPass = simulatingRoute.Clone();
                                bs.Add(new Bus(int.Parse(IDList[i]),routesToPass, r, int.Parse(UpdateSpeedBox.Text), false, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value)));
                            }
                            break;
                        case 1:
                            for (int i = 0; i < IDList.Count; i++)
                            {
                                routesToPass = simulatingRoute.Clone();
                                bs.Add(new Bus(int.Parse(IDList[i]), routesToPass, r, int.Parse(UpdateSpeedBox.Text), true, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value)));
                            }
                            break;
                        case 2:
                            for (int i = 0; i < IDList.Count; i++)
                            {
                                int test = r.Next(1, 3);
                                if (test == 1)
                                    desc = true;
                                else
                                    desc = false;
                                routesToPass = simulatingRoute.Clone();
                                bs.Add(new Bus(int.Parse(IDList[i]), routesToPass, r, int.Parse(UpdateSpeedBox.Text), desc, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value)));
                            }

                            break;
                        default:
                            break;
                    }
                    break;
                case 2:
                    List<string> IDFKList = DatabaseAcces.Query(AllBusQuery, new List<string>() { "ID","fk_BusRoute" });
                    switch (BusDirectionCombo.SelectedIndex)
                    {
                        case 0:
                            for (int i = 0; i < IDFKList.Count; i=i+2)
                            {
                                if (IDFKList[i+1].ToString() == BusNrCombo.SelectedIndex.ToString())
                                    desc = false;
                                else
                                {
                                    if (r.Next(1, 3) == 1)
                                        desc = true;
                                    else
                                        desc = false;
                                }
                                List<BusRoute> l = allRoutes.FindAll(R => R.id == IDFKList[i + 1].ToString());
                                routesToPass = l.Clone<BusRoute>();
                                bs.Add(new Bus(int.Parse(IDFKList[i]), routesToPass, r, int.Parse(UpdateSpeedBox.Text), desc, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value)));
                            }
                            break;
                        case 1:
                            for (int i = 0; i < IDFKList.Count; i = i + 2)
                            {
                                if (IDFKList[i+1].ToString() == BusNrCombo.SelectedIndex.ToString())
                                    desc = true;
                                else
                                {
                                    if (r.Next(1, 3) == 1)
                                        desc = true;
                                    else
                                        desc = false;
                                }
                                List<BusRoute> l = allRoutes.FindAll(R => R.id == IDFKList[i + 1].ToString());
                                routesToPass = l.Clone<BusRoute>();
                                bs.Add(new Bus(int.Parse(IDFKList[i]), routesToPass, r, int.Parse(UpdateSpeedBox.Text), desc, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value)));
                            }

                            break;
                        case 2:
                            for (int i = 0; i < IDFKList.Count; i = i + 2)
                            {
                                if (r.Next(1, 3) == 1)
                                    desc = true;
                                else
                                    desc = false;
                                List<BusRoute> l = allRoutes.FindAll(R => R.id == IDFKList[i + 1].ToString());
                                routesToPass = l.Clone<BusRoute>();
                                bs.Add(new Bus(int.Parse(IDFKList[i]), routesToPass, r, int.Parse(UpdateSpeedBox.Text), desc, ((int)MinSpeedSLider.Value), ((int)MaxSpeedSLider.Value)));
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
                
            }
            busList = bs;
        }

        private void Truncate()
        {
            string query = "Truncate GPSPosition";
            LogText.AppendText("Removing all GPS positions\n");
            DatabaseAcces.InsertOrUpdate(query);

        }

        private void ClearLogButton_click(object sender, RoutedEventArgs e)
        {
            LogText.Document.Blocks.Clear();
        }
        private void RefreshButton_click(object sender, RoutedEventArgs e)
        {
            GetBusRoutesInSystem();
        }

        public int GetBusRoutesInSystem()
        {
            BusNrCombo.Items.Clear();
            string query = "select distinct BusRoute.RouteNumber from BusRoute "+
                           "inner join Bus on Bus.fk_BusRoute = BusRoute.ID "+
                           "inner join BusRoute_RoutePoint on BusRoute.ID = BusRoute_RoutePoint.fk_BusRoute";
            List<string> BusRoutes = DatabaseAcces.Query(query, new List<string>(){"RouteNumber"});
            foreach(string s in BusRoutes)
            {
                BusNrCombo.Items.Add(s);
            }
            if (BusNrCombo.Items.Count == 0)
            {
                    LogText.AppendText("*ERROR* - No BusRoutes in database");
                    SimuModeCombo.IsEnabled = false;
                    return -1;
            }
            SimuModeCombo.IsEnabled = true;
            AddSimuModes();
            return 0;
            
        }

        public void AddSimuModes()
        {
            SimuModeCombo.Items.Clear();
            SimuModeCombo.Items.Add("Use single bus on chosen route");
            SimuModeCombo.Items.Add("Use all busses on chosen route");
            SimuModeCombo.Items.Add("Use all busses in system");
        }

        public void AddDirection()
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

        private void LogText_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            LogText.ScrollToEnd();
        }

        private void BusNrCombo_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (SimuModeCombo.SelectedItem != null && BusNrCombo.SelectedItem != null)
            {
                AddDirection();
            }
        }

        private void SimuModeCombo_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (SimuModeCombo.SelectedItem != null && BusNrCombo.SelectedItem != null)
            {
                AddDirection();
            }
         
        }

        private void MinSpeedSLider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            foreach (Bus b in busList)
            {
                b.minSpeed = (int)e.NewValue;
            }
            MinSpeedBlock.Text = "Minimum speed: " + ((int)e.NewValue).ToString();
        }

        private void MaxSpeedSLider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            foreach (Bus b in busList)
            {
                b.maxSpeed = (int)e.NewValue;
            }
            MinSpeedSLider.Maximum = ((int)e.NewValue);
            MaxSpeedBlock.Text = "Maximum speed: " + ((int)e.NewValue).ToString();
        }

        private bool Wait = false;
        private void HandleDataUpdate(object sender, SendLogMessageEventArgs e)
        {
            while(Wait)
            {
                Thread.Sleep(10);
            }
            Wait = true;
            this.Dispatcher.BeginInvoke(new Action(() => { LogText.AppendText(e.msgData); }));
            Wait = false;
            
        
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (Bus b in busList)
            {
                b.stopSim();
            }
        }
    }
}
