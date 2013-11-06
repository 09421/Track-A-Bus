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
        private List<Tuple<string,string>> busRoute;
        private Random r = new Random();

        public MainWindow()
        {
            InitializeComponent();
            string connString = ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString();
            conn = new MySqlConnection(connString);

            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
        }
        

        public bool OpenConnection()
        {
            try{
                LogText.AppendText("Opening connection" + Environment.NewLine);
                conn.Open();
            }
            catch(Exception e)
            {
                LogText.AppendText(e.ToString());
                    return false;
            }
            LogText.AppendText("Connection open! \n");
            return true;
        }
        public void CloseConnection()
        {
            conn.Close();
            LogText.AppendText("Connection Closed" + Environment.NewLine);
        }

        public void TestSP()
        {
            int test;
            MySqlCommand sp = new MySqlCommand("CalcBusToStopTime", conn);
            sp.CommandType = System.Data.CommandType.StoredProcedure;
            sp.Parameters.Add(new MySqlParameter("BusStopId", 50));
            sp.Parameters.Add(new MySqlParameter("routeNumber", 5));
            sp.Parameters.Add(new MySqlParameter("TimeToStopSec", MySqlDbType.Int64));
            sp.Parameters["TimeToStopSec"].Direction = System.Data.ParameterDirection.Output;
            OpenConnection();


                int r = sp.ExecuteNonQuery();
                test = int.Parse(sp.Parameters["TimeToStopSec"].Value.ToString());
                LogText.AppendText(test.ToString());

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
                getRouteFromID(bnID);
                InsertBusses(bnID.ToString());
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
            int count = 0;
            string bnID = "";
            string busNr = BusNrBox.Text;
            int uSpeed = 0;
            int BusOnRoute = 0;
            int RunningBusses = 0;
            if(!int.TryParse(UpdateSpeedBox.Text,out uSpeed) && !int.TryParse(BussesOnRouteNrBox.Text, out BusOnRoute) &&
                !int.TryParse(RunningBussesNrBox.Text,out RunningBusses))
            {
                LogText.AppendText("*ERROR* - Please do not leave any empty boxes\n");
                return -1;
            }
            if( uSpeed <= 0)
            {
                LogText.AppendText("*ERROR* - Update speed cannot be lower or equal to zero\n");
            }
            if (BusOnRoute > RunningBusses)
            {
                LogText.AppendText("*ERROR* - Number of busses on route is greater than total number of busses\r\n");
                return -1;
            }


            using (MySqlCommand query = new MySqlCommand())
            {
                query.CommandText = "SELECT busNumberId FROM BusNumbers where busNumber=" + busNr + ";";
                query.Connection = conn;
                try
                {
                    MySqlDataReader reader = query.ExecuteReader();

                    while (reader.Read())
                    {
                        count++;
                        bnID = reader["busNumberId"].ToString();
                    }
                    if (count != 1)
                    {
                        LogText.AppendText("*ERROR* - Something is wrong with the busnumber\r\n");
                        return -1;
                    }
                    reader.Close();
                }
                catch (Exception e)
                {
                    LogText.AppendText("*ERROR* - Failure to communicate with database");
                    return -1;
                }
            }
            LogText.AppendText("Configuration OK" + Environment.NewLine);
            return int.Parse(bnID);
        }
        private void InsertBusses(string bnID)
        {
            int TotalBusses = int.Parse(RunningBussesNrBox.Text);
            int RouteBusses = int.Parse(BussesOnRouteNrBox.Text);
            int uSpeed = int.Parse(UpdateSpeedBox.Text);
            string QueryText = "Insert into Busses (busId, fk_busNumberId) values ";
            string logText = "Inserting (BusId,BusNrId): ";
            List<string> bnIDs = getAllBusNumberId();
            for (int i = 1; i <= TotalBusses; i++)
            {

                if (i <= RouteBusses)
                {
                    QueryText += "(" + i.ToString() + ", " + bnID + "),";
                    BusSimu bs = new BusSimu(i, int.Parse(bnID), busRoute, LogText, this, r, uSpeed);
                    busList.Add(bs);
                }
                else
                {
                    
                    int index = r.Next(bnIDs.Count());
                    while (bnIDs[index] == bnID)
                    {
                        index = r.Next(bnIDs.Count-1);
                    }
                    QueryText += "(" + i.ToString() + ", " + bnIDs[index] + "),";
                    logText += "(" + i.ToString() + ", " + bnIDs[index] +"), ";

                    BusSimu bs = new BusSimu(i, int.Parse(bnIDs[index]), busRoute, LogText, this, r, uSpeed);
                    busList.Add(bs);
                }
            }
            QueryText = QueryText.Remove(QueryText.Length-1, 1);
            logText = logText.Remove(logText.Length-2,2);
            LogText.AppendText(logText + Environment.NewLine);
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = QueryText;
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
            }
        }

        private void Truncate()
        {
            string query1 = "Truncate Busses";
            string query2 = "Truncate GPSPos";
            LogText.AppendText("Removing all busses and positions\n");

            busList.Clear();
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = query1;
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
            }
         
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = query2;
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
            }
        }

        private List<string> getAllBusNumberId()
        {
            List<string> bnIDs = new List<string>();
            string query = "Select busNumberId from BusNumbers";
            using(MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = query;
                cmd.Connection = conn;
                MySqlDataReader reader = cmd.ExecuteReader();
                while(reader.Read())
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

        private void getRouteFromID(int bnID)
        {
            busRoute = new List<Tuple<string, string>>();
            string query = "SELECT bus_lat, bus_lon FROM BusRoutePoints " +
                            "join BusNumberToRoutePoints on " +
                            "BusNumberToRoutePoints.busRoutePointId=BusRoutePoints.id " +
                            "where BusNumberToRoutePoints.busNumberId=" + bnID.ToString();
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = query;
                cmd.Connection = conn;
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    busRoute.Add(new Tuple<string, string>(reader["bus_lat"].ToString(), reader["bus_lon"].ToString()));
                }
                reader.Close();
            }
        }

        private void LogText_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            LogText.ScrollToEnd();
        }

    }
}
