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
        private string ConnString;
        private bool isRunning = false;
        private bool testSimu = false;
        private MySqlConnection conn;

        private List<string> busID;
        private List<BusSimu> busList = new List<BusSimu>();
        private List<Tuple<string, string>> busRoute;
        private Random r = new Random();

        public MainWindow()
        {
            InitializeComponent();
            string connString = ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString();
            conn = new MySqlConnection(connString);
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            GetBusRoutesInSystem();
            AddSimuModes();
        }


        public bool OpenConnection()
        {
            try
            {

                conn.Open();
            }
            catch (Exception e)
            {
                LogText.AppendText("*ERROR* - Failure to connect to database\n");
                return false;
            }
            return true;
        }
        public void CloseConnection()
        {
            conn.Close();
        }


        private void StartStopButton_Click_1(object sender, RoutedEventArgs e)
        {

            if (!isRunning)
            {
                OpenConnection();
                int bnID = CheckVal();
                if (bnID == -1)
                {
                    CloseConnection();
                    return;
                }
                Truncate();
                CreateBusses(bnID.ToString());
                StartStopButton.Background = Brushes.Red;
                StartStopButton.Content = "Stop Simulation";
                foreach (BusSimu bs in busList)
                {
                    bs.startSim();
                    testSimu = true;
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
                    testSimu = false;
                    conn.Close();
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

            string query = "select ID from BusRoute where RouteNumber = '" + BusNrCombo.SelectedItem.ToString() + "'";
            LogText.AppendText("Configuration OK" + Environment.NewLine);
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = query;
                MySqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                return int.Parse(reader["ID"].ToString());
            }

        }

        private void CreateBusses(string bnID)
        {
            List<BusSimu> bs = new List<BusSimu>();
            string singleBusQuery = "Select ID from Bus where fk_BusRoute = "+bnID + "limit 1";
            string allBusOnRouteQuery = "Select ID from Bus where fk_BusRoute = " +bnID;
            string AllBusQuery = "Select ID, fk_BusRoute from Bus where fk_BusRoute is not null";
            bool desc = false;
            switch(SimuModeCombo.SelectedIndex)
            {
                case 0:
                    switch(BusDirectionCombo.SelectedIndex)
                    {
                        case 0:
                            using(MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.CommandText = singleBusQuery;
                                cmd.Connection = conn;
                                MySqlDataReader reader = cmd.ExecuteReader();
                                reader.Read();
                                bs.Add(new BusSimu(int.Parse(reader["ID"].ToString()), int.Parse(bnID),
                                    getRouteFromID(int.Parse(bnID), false), LogText, this, r, int.Parse(UpdateSpeedBox.Text)));
                                setBusDirectionDB(int.Parse(reader["ID"].ToString()), false);
                            }
                            break;
                        case 1:
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.CommandText = singleBusQuery;
                                cmd.Connection = conn;
                                MySqlDataReader reader = cmd.ExecuteReader();
                                reader.Read();
                                bs.Add(new BusSimu(int.Parse(reader["ID"].ToString()), int.Parse(bnID),
                                    getRouteFromID(int.Parse(bnID), true), LogText, this, r, int.Parse(UpdateSpeedBox.Text)));
                                setBusDirectionDB(int.Parse(reader["ID"].ToString()), true);
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case 1:
                    switch (BusDirectionCombo.SelectedIndex)
                    {
                        case 0:
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.CommandText = allBusOnRouteQuery;
                                cmd.Connection = conn;
                                MySqlDataReader reader = cmd.ExecuteReader();
                                while(reader.Read())
                                {
                                    bs.Add(new BusSimu(int.Parse(reader["ID"].ToString()), int.Parse(bnID),
                                    getRouteFromID(int.Parse(bnID), false), LogText, this, r, int.Parse(UpdateSpeedBox.Text)));
                                    setBusDirectionDB(int.Parse(reader["ID"].ToString()), false);                                   
                                }
                            }
                            break;
                        case 1:
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.CommandText = allBusOnRouteQuery;
                                cmd.Connection = conn;
                                MySqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    bs.Add(new BusSimu(int.Parse(reader["ID"].ToString()), int.Parse(bnID),
                                    getRouteFromID(int.Parse(bnID), true), LogText, this, r, int.Parse(UpdateSpeedBox.Text)));
                                    setBusDirectionDB(int.Parse(reader["ID"].ToString()), true);
                                }
                            }
                            break;
                        case 2:

                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.CommandText = allBusOnRouteQuery;
                                cmd.Connection = conn;
                                MySqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    if(r.Next(1,2) == 1)
                                        desc = true;
                                    else
                                        desc = false;
                                    bs.Add(new BusSimu(int.Parse(reader["ID"].ToString()), int.Parse(bnID),
                                    getRouteFromID(int.Parse(bnID), desc), LogText, this, r, int.Parse(UpdateSpeedBox.Text)));
                                    setBusDirectionDB(int.Parse(reader["ID"].ToString()), desc);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case 2:                    
                    switch (BusDirectionCombo.SelectedIndex)
                    {
                        case 0:                            
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.CommandText = AllBusQuery;
                                cmd.Connection = conn;
                                MySqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    if (reader["fk_BusRoute"].ToString() == bnID)
                                        desc = false;
                                    else
                                    {
                                        if (r.Next(1, 2) == 1)
                                            desc = true;
                                        else
                                            desc = false;   
                                    }
                                    bs.Add(new BusSimu(int.Parse(reader["ID"].ToString()), int.Parse(reader["fk_BusRoute"].ToString()),
                                    getRouteFromID(int.Parse(reader["fk_BusRoute"].ToString()), desc), LogText, this, r, int.Parse(UpdateSpeedBox.Text)));
                                    setBusDirectionDB(int.Parse(reader["ID"].ToString()), desc);
                                }
                            }
                            break;
                        case 1:
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.CommandText = AllBusQuery;
                                cmd.Connection = conn;
                                MySqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    if (reader["fk_BusRoute"].ToString() == bnID)
                                        desc = true;
                                    else
                                    {
                                        if (r.Next(1, 2) == 1)
                                            desc = true;
                                        else
                                            desc = false;
                                    }
                                    bs.Add(new BusSimu(int.Parse(reader["ID"].ToString()), int.Parse(reader["fk_BusRoute"].ToString()),
                                    getRouteFromID(int.Parse(reader["fk_BusRoute"].ToString()), desc), LogText, this, r, int.Parse(UpdateSpeedBox.Text)));
                                    setBusDirectionDB(int.Parse(reader["ID"].ToString()), desc);
                                }
                            }
                            break;
                        case 2:
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.CommandText = AllBusQuery;
                                cmd.Connection = conn;
                                MySqlDataReader reader = cmd.ExecuteReader();
                                while (reader.Read())
                                {
                                    if (r.Next(1, 2) == 1)
                                        desc = true;
                                    else
                                        desc = false;
                                    bs.Add(new BusSimu(int.Parse(reader["ID"].ToString()), int.Parse(reader["fk_BusRoute"].ToString()),
                                    getRouteFromID(int.Parse(reader["fk_BusRoute"].ToString()), desc), LogText, this, r, int.Parse(UpdateSpeedBox.Text)));
                                    setBusDirectionDB(int.Parse(reader["ID"].ToString()), desc);
                                }
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

        private void Truncate()
        {
            string query1 = "Truncate GPSPosition";
            LogText.AppendText("Removing all GPS positions\n");

            busList.Clear();
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = query1;
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
            }

        }

        private List<string> getAllBusNumberId()
        {
            List<string> bnIDs = new List<string>();
            string query = "Select busNumberId from BusNumbers";
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = query;
                cmd.Connection = conn;
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    bnIDs.Add(reader["busNumberId"].ToString());
                }
                reader.Close();
            }
            return bnIDs;
        }

        private void TestButton_click(object sender, RoutedEventArgs e)
        {
            LogText.Document.Blocks.Clear();
        }

        private List<Tuple<string, string>> getRouteFromID(int bnID, bool isDescending)
        {
            List<Tuple<string, string>> route= new List<Tuple<string, string>>();
            string query = "";
            if (isDescending)
            {
                query = "SELECT Latitude, Longitude FROM RoutePoint " +
                               "join BusRoute_RoutePoint on " +
                               "BusRoute_RoutePoint.fk_BusRoute=BusRoute.ID " +
                               "where BusRoute_RoutePoint.fk_BusRoute=" + bnID.ToString() +
                               "order by BusRoute_RoutePoint.fk_RoutePoint desc";
            }
            else
            {
                query = "SELECT Latitude, Longitude FROM RoutePoint " +
                               "join BusRoute_RoutePoint on " +
                               "BusRoute_RoutePoint.fk_BusRoute=BusRoute.ID " +
                               "where BusRoute_RoutePoint.fk_BusRoute=" + bnID.ToString() +
                               " order by BusRoute_RoutePoint.fk_RoutePoint asc";
            }
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = query;
                cmd.Connection = conn;
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    route.Add(new Tuple<string, string>(reader["Latitude"].ToString(), reader["Longitude"].ToString()));
                }
                reader.Close();
            }
            return route;
        }

        public void setBusDirectionDB(int bID, bool isDescending)
        {
            string query = "Update Bus set IsDescending = " + isDescending.ToString() +
                           " where Bus.ID = " + bID.ToString();
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = query;
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
            }
        }

        public int GetBusRoutesInSystem()
        {
            OpenConnection();
            string query = "Select RouteNumber from BusRoute " +
                           "inner join Bus on Bus.fk_BusRoute=BusRoute.ID";
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = query;
                cmd.Connection = conn;
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    BusNrCombo.Items.Add(reader["RouteNumber"].ToString());
                }
                CloseConnection();
                if (BusNrCombo.Items.Count == 0)
                {
                    LogText.AppendText("*ERROR* - No BusRoutes in database");
                    return -1;
                }
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
            BusDirectionCombo.Items.Clear();
            string bnID = "";
            string routeEndPointAsc = "";
            string routeEndPointDesc = "";
            string idQuery = "Select ID from BusRoute where RouteNumber = '" + BusNrCombo.SelectedItem.ToString()+"'";

            OpenConnection();
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = idQuery;
                cmd.Connection = conn;
                MySqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                bnID = reader["ID"].ToString();
                reader.Close();
            }
            string endPointAscQuery = "Select StopName from BusStop " +
                "join BusRoute_BusStop on BusStop.ID = BusRoute_BusStop.fk_BusStop " +
                "where BusRoute_BusStop.fk_BusRoute = " + bnID + " " +
                "order by BusStop.ID asc limit 1";
            string endPointDescQuery = "Select StopName from BusStop " +
                            "join BusRoute_BusStop on BusStop.ID = BusRoute_BusStop.fk_BusStop " +
                            "where BusRoute_BusStop.fk_BusRoute = " + bnID + " " +
                            "order by BusStop.ID desc limit 1";

            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = endPointDescQuery;
                cmd.Connection = conn;
                MySqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                routeEndPointDesc = reader["StopName"].ToString();

            }
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = endPointAscQuery;
                cmd.Connection = conn;
                MySqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                routeEndPointAsc = reader["StopName"].ToString();
            }
            CloseConnection();
            switch (SimuModeCombo.SelectedIndex)
            {
                case 0:
                    BusDirectionCombo.Items.Add("Bus traveling towards " + routeEndPointDesc);
                    BusDirectionCombo.Items.Add("Bus traveling towards " + routeEndPointAsc);
                    break;
                case 1:
                    BusDirectionCombo.Items.Add("All Busses traveling towards " + routeEndPointDesc);
                    BusDirectionCombo.Items.Add("All Busses traveling towards " + routeEndPointAsc);
                    BusDirectionCombo.Items.Add("Randomize direction");
                    break;
                case 2:
                    BusDirectionCombo.Items.Add("All busses on route traveling towards " + routeEndPointDesc);
                    BusDirectionCombo.Items.Add("All busses on route traveling towards " + routeEndPointAsc);
                    BusDirectionCombo.Items.Add("Randomize direction");
                    break;
                default:
                    break;
            }

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

    }
}
