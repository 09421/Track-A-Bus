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
using MySql.Data.MySqlClient;

namespace GPSsimu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isRunning = false;
        private List<BusSimu> busList = new List<BusSimu>();
        private List<Tuple<string, string>> simulatingRoute = new List<Tuple<string, string>>();
        private List<Tuple<string, string>> allRoutes = new List<Tuple<string, string>>();
        private Random r = new Random();

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
                foreach (BusSimu bs in busList)
                {
                    bs.startSim();
                }
                isRunning = true;
            }
            else
            {
                StartStopButton.Background = Brushes.Green;
                StartStopButton.Content = "Start Simulation";
                foreach (BusSimu bs in busList)
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

        private List<Tuple<string, string>> CreateAllBusRoutes()
        {
            List<Tuple<string, string>> Routes = new List<Tuple<string, string>>();
            string query = "select ID, RouteNumber from BusRoute";

            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Routes.Add(new Tuple<string,string>(reader["ID"].ToString(), reader["RouteNumber"].ToString()));
                    }
                    reader.Close();
                    conn.Close();
                }
                return Routes;
            }
        }

        private List<Tuple<string,string>> CreateSimuBusRoutes()
        {

            List<Tuple<string, string>> Routes = new List<Tuple<string, string>>();
            string query = "select ID from BusRoute where RouteNumber = '" + BusNrCombo.SelectedItem.ToString() + "'";
   
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using(MySqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Routes.Add(new Tuple<string, string>(reader["ID"].ToString(), BusNrCombo.SelectedItem.ToString()));
                    }
                    reader.Close();
                    conn.Close();
                }
                return Routes;
            }
        }

        private void CreateBusses()
        {
            bool desc = false;
            int id = 0;
            List<BusSimu> bs = new List<BusSimu>();
            List<int> IDList = new List<int>();
            List<int> fkBusRouteList = new List<int>();
            List<Tuple<string, string>> routeList = new List<Tuple<string, string>>();


            
            string singleBusQuery = "Select ID from Bus where ";
            string allBusOnRouteQuery = "Select ID from Bus where ";
            string AllBusQuery = "Select ID, fk_BusRoute from Bus where fk_BusRoute is not null";
            foreach (Tuple<string,string> r in simulatingRoute)
            {
                singleBusQuery += "fk_busRoute = " + r.Item1 + " or ";
                allBusOnRouteQuery += "fk_busRoute = " + r.Item1 + " or ";
            }

            singleBusQuery = singleBusQuery.TrimEnd(" or ".ToCharArray()) + " limit 1";
            allBusOnRouteQuery = allBusOnRouteQuery.TrimEnd(" or ".ToCharArray());
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                switch (SimuModeCombo.SelectedIndex)
                {
                    case 0:
                        using (MySqlCommand cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandText = singleBusQuery;
                            MySqlDataReader reader = cmd.ExecuteReader();
                            reader.Read();
                            id = int.Parse(reader["ID"].ToString());
                            reader.Close();
                            conn.Close();
                        }
                        switch (BusDirectionCombo.SelectedIndex)
                        {
                            case 0:
                                bs.Add(new BusSimu(id, simulatingRoute, LogText, this, r, int.Parse(UpdateSpeedBox.Text), false));
                                break;
                            case 1:
                                bs.Add(new BusSimu(id, simulatingRoute, LogText, this, r, int.Parse(UpdateSpeedBox.Text), true));
                                break;
                            default:
                                break;
                        }
                        break;
                    case 1:
                        using (MySqlCommand cmd = conn.CreateCommand())
                         {
                            conn.Open();
                            cmd.CommandText = allBusOnRouteQuery;
                            MySqlDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                IDList.Add(int.Parse(reader["ID"].ToString()));
                            }
                            reader.Close();
                            conn.Close();
                        }
                        switch (BusDirectionCombo.SelectedIndex)
                        {
                            case 0:
                                for (int i = 0; i < IDList.Count; i++)
                                {
                                    bs.Add(new BusSimu(IDList[i],simulatingRoute, LogText, this, r, int.Parse(UpdateSpeedBox.Text),false));
                                }
                                break;
                            case 1:
                                for (int i = 0; i < IDList.Count; i++)
                                {
                                    bs.Add(new BusSimu(IDList[i],simulatingRoute, LogText, this, r, int.Parse(UpdateSpeedBox.Text),true));
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
                                    bs.Add(new BusSimu(IDList[i], simulatingRoute, LogText, this, r, int.Parse(UpdateSpeedBox.Text), desc));
                                }
                                
                                break;
                            default:
                                break;
                        }
                        break;
                    case 2:
                        using (MySqlCommand cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandText = AllBusQuery;
                            cmd.Connection = conn;
                            MySqlDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                IDList.Add(int.Parse(reader["ID"].ToString()));
                                fkBusRouteList.Add(int.Parse(reader["fk_BusRoute"].ToString()));
                            }
                            reader.Close();
                            conn.Close();
                        }

                        switch (BusDirectionCombo.SelectedIndex)
                        {
                            case 0:
                                for (int i = 0; i < IDList.Count; i++)
                                {
                                    if (fkBusRouteList[i].ToString() == BusNrCombo.SelectedIndex.ToString())
                                        desc = false;
                                    else
                                    {
                                        if (r.Next(1, 3) == 1)
                                            desc = true;
                                        else
                                            desc = false;
                                    }
                                    List<Tuple<string,string>> l = allRoutes.FindAll(R => R.Item2 == fkBusRouteList[i].ToString());
                                    bs.Add(new BusSimu(IDList[i], l, LogText, this, r, int.Parse(UpdateSpeedBox.Text), desc));
                                }
                                break;
                            case 1:
                                for (int i = 0; i < IDList.Count; i++)
                                {
                                    if (fkBusRouteList[i].ToString() == BusNrCombo.SelectedIndex.ToString())
                                        desc = true;
                                    else
                                    {
                                        if (r.Next(1, 3) == 1)
                                            desc = true;
                                        else
                                            desc = false;
                                    }
                                    List<Tuple<string, string>> l = allRoutes.FindAll(R => R.Item2 == fkBusRouteList[i].ToString());
                                    bs.Add(new BusSimu(IDList[i], l, LogText, this, r, int.Parse(UpdateSpeedBox.Text), desc));
                                }
                                
                                break;
                            case 2:
                                for (int i = 0; i < IDList.Count; i++)
                                {
                                    if (r.Next(1, 3) == 1)
                                        desc = true;
                                    else
                                        desc = false;
                                    List<Tuple<string, string>> l = allRoutes.FindAll(R => R.Item2 == fkBusRouteList[i].ToString());
                                    bs.Add(new BusSimu(IDList[i], l, LogText, this, r, int.Parse(UpdateSpeedBox.Text), desc));
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
        
            busList = bs;
        }

        private void Truncate()
        {
            string query1 = "Truncate GPSPosition";
            LogText.AppendText("Removing all GPS positions\n");

            busList.Clear();
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        conn.Open();
                        cmd.CommandText = query1;
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                    catch (Exception e)
                    {
                        conn.Close();
                    }

                }
            }


        }

        //private List<string> getAllBusNumberId()
        //{
        //    List<string> bnIDs = new List<string>();
        //    string query = "Select busNumberId from BusNumbers";
        //    using (MySqlCommand cmd = new MySqlCommand())
        //    {
        //        cmd.CommandText = query;
        //        cmd.Connection = conn;
        //        MySqlDataReader reader = cmd.ExecuteReader();
        //        while (reader.Read())
        //        {
        //            bnIDs.Add(reader["busNumberId"].ToString());
        //        }
        //        reader.Close();
        //    }
        //    return bnIDs;
        //}

        private void ClearLogButton_click(object sender, RoutedEventArgs e)
        {
            LogText.Document.Blocks.Clear();
        }
        private void RefreshButton_click(object sender, RoutedEventArgs e)
        {
            GetBusRoutesInSystem();
        }

        //private <List<List<Tuple<string, string>>> getRouteFromID(int bnID, bool isDescending)
        //{
        //    List<Tuple<string, string>> route= new List<Tuple<string, string>>();
        //    string query = "";
        //    if (isDescending)
        //    {
        //        query = "SELECT Latitude, Longitude FROM RoutePoint " +
        //                       "join BusRoute_RoutePoint on " +
        //                       "BusRoute_RoutePoint.fk_RoutePoint=RoutePoint.ID " +
        //                       "where BusRoute_RoutePoint.fk_BusRoute=" + bnID.ToString() + " and " +
        //                       "RoutePoint.ID not in (select BusStop.fk_RoutePoint from BusStop) " +
        //                       " order by BusRoute_RoutePoint.ID desc";
        //    }
        //    else
        //    {
        //        query = "SELECT Latitude, Longitude FROM RoutePoint " +
        //                       "join BusRoute_RoutePoint on " +
        //                       "BusRoute_RoutePoint.fk_RoutePoint=RoutePoint.ID " +
        //                       "where BusRoute_RoutePoint.fk_BusRoute=" + bnID.ToString() + " and " +
        //                       "RoutePoint.ID not in (select BusStop.fk_RoutePoint from BusStop) " +
        //                       " order by BusRoute_RoutePoint.ID asc";
        //    }
        //    using (MySqlCommand cmd = new MySqlCommand())
        //    {
        //        cmd.CommandText = query;
        //        cmd.Connection = conn;
        //        MySqlDataReader reader = cmd.ExecuteReader();
        //        while (reader.Read())
        //        {
        //            route.Add(new Tuple<string, string>(reader["Latitude"].ToString(), reader["Longitude"].ToString()));
        //        }
        //        reader.Close();
        //    }
        //    return route;
        //}

        //public void setBusDirectionDB(int bID, bool isDescending)
        //{
        //    string query = "Update Bus set IsDescending = " + isDescending.ToString() +
        //                   " where Bus.ID = " + bID.ToString();
        //    using (MySqlCommand cmd = new MySqlCommand())
        //    {
        //        cmd.CommandText = query;
        //        cmd.Connection = conn;
        //        cmd.ExecuteNonQuery();
        //    }
        //}

        public int GetBusRoutesInSystem()
        {
            string query = "Select distinct RouteNumber from BusRoute " +
                           "inner join Bus on Bus.fk_BusRoute=BusRoute.ID";
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        BusNrCombo.Items.Add(reader["RouteNumber"].ToString());
                    }
                    reader.Close();
                    conn.Close();
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
        }

        public void AddSimuModes()
        {
            SimuModeCombo.Items.Add("Use single bus on chosen route");
            SimuModeCombo.Items.Add("Use all busses on chosen route");
            SimuModeCombo.Items.Add("Use all busses in system");
        }

        public void AddDirection()
        {
            //BusDirectionCombo.Items.Clear();
            //string bnID = "";
            //string routeEndPointAsc = "";
            //string routeEndPointDesc = "";
            //string idQuery = "Select ID from BusRoute where RouteNumber = '" + BusNrCombo.SelectedItem.ToString()+"'";

            //OpenConnection();
            //using (MySqlCommand cmd = new MySqlCommand())
            //{
            //    cmd.CommandText = idQuery;
            //    cmd.Connection = conn;
            //    MySqlDataReader reader = cmd.ExecuteReader();
            //    reader.Read();
            //    bnID = reader["ID"].ToString();
            //    reader.Close();
            //}
            //string endPointAscQuery = "Select StopName from BusStop " +
            //    "join BusRoute_BusStop on BusStop.ID = BusRoute_BusStop.fk_BusStop " +
            //    "where BusRoute_BusStop.fk_BusRoute = " + bnID + " " +
            //    "order by BusStop.ID asc limit 1";
            //string endPointDescQuery = "Select StopName from BusStop " +
            //                "join BusRoute_BusStop on BusStop.ID = BusRoute_BusStop.fk_BusStop " +
            //                "where BusRoute_BusStop.fk_BusRoute = " + bnID + " " +
            //                "order by BusStop.ID desc limit 1";

            //using (MySqlCommand cmd = new MySqlCommand())
            //{
            //    cmd.CommandText = endPointDescQuery;
            //    cmd.Connection = conn;
            //    MySqlDataReader reader = cmd.ExecuteReader();
            //    reader.Read();
            //    routeEndPointDesc = reader["StopName"].ToString();
            //    reader.Close();

            //}
            //using (MySqlCommand cmd = new MySqlCommand())
            //{
            //    cmd.CommandText = endPointAscQuery;
            //    cmd.Connection = conn;
            //    MySqlDataReader reader = cmd.ExecuteReader();
            //    reader.Read();
            //    routeEndPointAsc = reader["StopName"].ToString();
            //    reader.Close();
            //}
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
            if (SimuModeCombo.SelectedItem != null && BusNrCombo.SelectedItem != null && !BusDirectionCombo.IsEnabled)
            {
                AddDirection();
            }
        }

        private void SimuModeCombo_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (SimuModeCombo.SelectedItem != null && BusNrCombo.SelectedItem != null && !BusDirectionCombo.IsEnabled)
            {
                AddDirection();
            }
         
        }

        private void MinSpeedSLider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            foreach (BusSimu b in busList)
            {
                b.minSpeed = (int)e.NewValue;
            }
            MinSpeedBlock.Text = "Minimum speed: " + ((int)e.NewValue).ToString();
        }

        private void MaxSpeedSLider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            foreach (BusSimu b in busList)
            {
                b.maxSpeed = (int)e.NewValue;
            }
            MinSpeedSLider.Maximum = ((int)e.NewValue);
            MaxSpeedBlock.Text = "Maximum speed: " + ((int)e.NewValue).ToString();
        }


        
    }
}
