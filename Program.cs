using System;
using System.IO.Ports;
using System.Threading;
using System.IO;
using System.Data.OleDb;
using System.Text;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace wczytywanie_z_pliku
{


    class Program
    {
        static bool _continue;
        static SerialPort _serialPort;
        static SerialPort _serialPort_COM2;
        static void Main(string[] args)
        {
            //### Połączenie z bazą danych ###
            MySqlConnection sqlconn;
            string connsqlstring = "Server=192.168.230.170;port=3306;username=user;password=mati;database=tcon";
            sqlconn = new MySqlConnection(connsqlstring);
            sqlconn.Open();
            String status_polacznenia = sqlconn.State.ToString();
            Console.WriteLine($"MySQL DB is: {status_polacznenia}");
                 
            string message;
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            Thread readThread = new Thread(Read);

            // Create a new SerialPort object with default settings.
            _serialPort = new SerialPort();
            _serialPort_COM2 = new SerialPort();

            // Allow the user to set the appropriate properties.
            _serialPort.PortName = SetPortName(_serialPort.PortName);
            _serialPort.BaudRate = SetPortBaudRate(_serialPort.BaudRate);
            _serialPort.Parity = SetPortParity(_serialPort.Parity);
            _serialPort.DataBits = SetPortDataBits(_serialPort.DataBits);
            _serialPort.StopBits = SetPortStopBits(_serialPort.StopBits);
            _serialPort.Handshake = SetPortHandshake(_serialPort.Handshake);

            _serialPort_COM2.PortName = SetPortName_COM2(_serialPort_COM2.PortName);
            _serialPort_COM2.BaudRate = SetPortBaudRate_COM2(_serialPort_COM2.BaudRate);
            _serialPort_COM2.Parity = SetPortParity_COM2(_serialPort_COM2.Parity);
            _serialPort_COM2.DataBits = SetPortDataBits_COM2(_serialPort_COM2.DataBits);
            _serialPort_COM2.StopBits = SetPortStopBits_COM2(_serialPort_COM2.StopBits);
            _serialPort_COM2.Handshake = SetPortHandshake_COM2(_serialPort_COM2.Handshake);

            // Set the read/write timeouts
            _serialPort.ReadTimeout = 1500;
            _serialPort.WriteTimeout = 1500;
            _serialPort_COM2.ReadTimeout = 1500;
            _serialPort_COM2.WriteTimeout = 1500;

            _serialPort.Open();
            _serialPort_COM2.Open();
            _continue = true;
            readThread.Start();

            Console.WriteLine("Type QUIT to exit");

            while (_continue)
            {

                message = "dupa";

                if (stringComparer.Equals("quit", message))
                {
                    _continue = false;
                }

            }

            readThread.Join();
            _serialPort.Close();
            _serialPort_COM2.Close();
        }

//*************** MATI HS. Reading form port 
        public static void Read()
        {
            // *** Wczytywanie nazwy linii z txt ***
            string linia_teksu = "";
            string[] porty = new string[3];
            int licznik = 0;
            System.IO.StreamReader file = new System.IO.StreamReader(@"line.txt");
            string line = file.ReadLine();

            file.Close();


            while (_continue)
            {
                DateTime now = DateTime.Now;
                int dzien = now.Day;
                int mies = now.Month;
                int rok = now.Year;
                string day = dzien.ToString();
                string mon = mies.ToString();
                string year = rok.ToString();
                int count;

                if (mies < 10)
                {
                    mon = $"0{mies}";
                }
                string DATA = $"{day}{mon}{year}TconLog";


                try
                {
                    string DATAin = _serialPort.ReadTo("\r\n");
                    string DATAin_COM2 = _serialPort_COM2.ReadTo("\r\n");
                    now = DateTime.Now;
                    string date_now = now.ToString("G");

                    MySqlConnection sqlconn;
                    string connsqlstring = "Server=192.168.230.170;port=3306;username=user;password=mati;database=tcon";
                    sqlconn = new MySqlConnection(connsqlstring);
                    sqlconn.Open();

                    using (StreamWriter streamW = new StreamWriter(($"logi/{DATA}.txt"), true))
                    {
                        streamW.WriteLine($"{now.ToString("G")};{DATAin};{DATAin_COM2};{line}");
                    }

                    string module = DATAin;
                    string EAJ = "";
                    string eaj = "EAJ";
                    int dlugosc_DATAin = DATAin.Length;
                    // {Nie skanuje bo no read, zrób warunek
                    // SERVO SLIDE w tabeli common musi mieć wartość inną niż NULL Lub 0 bo wtedy model jest traktowany jak TCON LESS


                    if (dlugosc_DATAin == 21)                   
                    {
                        EAJ = eaj.ToString() + module[9].ToString() + module[10].ToString() + module[11].ToString() + module[12].ToString() + module[13].ToString() + module[14].ToString() + module[15].ToString() + module[16].ToString();

                        using (var cmd = new MySqlCommand())
                        {
                            
                            cmd.Connection = sqlconn;
                            cmd.CommandText = $"SELECT `ServoSlide` FROM `common` WHERE `LGE_PN` = '{EAJ}'";

                            string dupa = cmd.CommandText = $"SELECT `ServoSlide` FROM `common` WHERE `LGE_PN` = '{EAJ}'";
                            cmd.ExecuteNonQuery();
                            count = Convert.ToInt32(cmd.ExecuteScalar());


                        }
                        if (count == 0)
                        {
                            DATAin_COM2 = "TCONLESS";
                        }
                    }

                    Console.WriteLine(DATAin);
                    Console.WriteLine(DATAin_COM2);
                    Console.WriteLine();

                    using (StreamWriter streamW = new StreamWriter(($"logi/{DATA}.txt"), true))
                    {
                        streamW.WriteLine($"{now.ToString("G")};{DATAin};{DATAin_COM2};{line}");
                    }
                    if (DATAin != "NOREAD")
                    {
                        using (var cmd = new MySqlCommand())
                        {
                            DateTime localDate = DateTime.Now;
                            string date = DateTime.Now.ToString("yyyy-MM-dd ");

                            cmd.Connection = sqlconn;
                            cmd.CommandText = "INSERT INTO bm_scan (`DATA`, `LCM`, `TCON`, `LINIA`) VALUES (@date_now, @DATAin, @DATAin_COM2, @line)";
                            cmd.Parameters.AddWithValue("@date_now", date_now);
                            cmd.Parameters.AddWithValue("@DATAin", DATAin);
                            cmd.Parameters.AddWithValue("@DATAin_COM2", DATAin_COM2);
                            cmd.Parameters.AddWithValue("@line", line);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (TimeoutException) { }

            }
        }
//********** Koniec Funkcji

        // Display Port values and prompt user to enter a port.
        public static string SetPortName(string defaultPortName)
        {
            string portName;

            // *** Wczytywanie nazwy portu COM z txt ***
            string linia_teksu = "";
            string[] porty = new string[3];
            int licznik = 0;

            System.IO.StreamReader file = new System.IO.StreamReader(@"config.txt");
            while ((linia_teksu = file.ReadLine()) != null)
            {
                if (licznik == 0)
                {
                    porty[1] = linia_teksu;
                }
                licznik++;
            }
            file.Close();
            // *** KONIEC ***

            /// Console.WriteLine("Available Ports:");
            //foreach (string s in SerialPort.GetPortNames())
            // {
            //   Console.WriteLine("   {0}", s);
            // }

            // Console.Write("Enter COM port value (Default: {0}): \n", defaultPortName);
            //portName = Console.ReadLine();
            portName = (porty[1]);
            if (portName == "" || !(portName.ToLower()).StartsWith("com"))
            {
                portName = defaultPortName;
            }
            return portName;
        }
        // Display BaudRate values and prompt user to enter a value.
        public static int SetPortBaudRate(int defaultPortBaudRate)
        {
            string baudRate;

            Console.Write("Baud Rate(default:{0}): \n", defaultPortBaudRate);
            //baudRate = Console.ReadLine();
            baudRate = "9600";
            if (baudRate == "")
            {
                baudRate = defaultPortBaudRate.ToString();
            }

            return int.Parse(baudRate);
        }

        // Display PortParity values and prompt user to enter a value.
        public static Parity SetPortParity(Parity defaultPortParity)
        {
            string parity;

            Console.WriteLine("Available Parity options:");
            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Parity value (Default: {0}): \n", defaultPortParity.ToString(), true);
            //parity = Console.ReadLine();
            parity = defaultPortParity.ToString();
            if (parity == "")
            {
                parity = defaultPortParity.ToString();
            }

            return (Parity)Enum.Parse(typeof(Parity), parity, true);
        }
        // Display DataBits values and prompt user to enter a value.
        public static int SetPortDataBits(int defaultPortDataBits)
        {
            string dataBits;

            Console.Write("Enter DataBits value (Default: {0}): ", defaultPortDataBits);
            //dataBits = Console.ReadLine();
            dataBits = defaultPortDataBits.ToString();
            if (dataBits == "")
            {
                dataBits = defaultPortDataBits.ToString();
            }

            return int.Parse(dataBits.ToUpperInvariant());
        }

        // Display StopBits values and prompt user to enter a value.
        public static StopBits SetPortStopBits(StopBits defaultPortStopBits)
        {
            string stopBits;

            Console.WriteLine("Available StopBits options:");
            foreach (string s in Enum.GetNames(typeof(StopBits)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter StopBits value (None is not supported and \n" +
             "raises an ArgumentOutOfRangeException. \n (Default: {0}):", defaultPortStopBits.ToString());
            //stopBits = Console.ReadLine();
            stopBits = defaultPortStopBits.ToString();
            if (stopBits == "")
            {
                stopBits = defaultPortStopBits.ToString();
            }

            return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
        }
        public static Handshake SetPortHandshake(Handshake defaultPortHandshake)
        {
            string handshake;

            Console.WriteLine("Available Handshake options:");
            foreach (string s in Enum.GetNames(typeof(Handshake)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Handshake value (Default: {0}):", defaultPortHandshake.ToString());
            //handshake = Console.ReadLine();
            handshake = defaultPortHandshake.ToString();
            if (handshake == "")
            {
                handshake = defaultPortHandshake.ToString();
            }

            return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
        }

        //COM2

        // Display Port values and prompt user to enter a port.
        public static string SetPortName_COM2(string defaultPortName)
        {
            string portName;


            // *** Wczytywanie nazwy portu COM z txt ***
            string linia_teksu = "";
            string[] porty = new string[3];
            int licznik = 0;

            System.IO.StreamReader file = new System.IO.StreamReader(@"config.txt");
            while ((linia_teksu = file.ReadLine()) != null)
            {
                if (licznik == 1)
                {
                    porty[2] = linia_teksu;
                }
                licznik++;
            }
            file.Close();
            // *** Koniec ***

            //Console.WriteLine("Available Ports:");
            //foreach (string s in SerialPort.GetPortNames())
            //{
            //  Console.WriteLine("   {0}", s);
            //}

            //Console.Write("Enter COM port value (Default: {0}): \n", defaultPortName);
            //portName = Console.ReadLine();
            //defaultPortName = "COM4";
            portName = porty[2];
            if (portName == "" || !(portName.ToLower()).StartsWith("com"))
            {
                portName = defaultPortName;
            }
            return portName;
        }
        // Display BaudRate values and prompt user to enter a value.
        public static int SetPortBaudRate_COM2(int defaultPortBaudRate)
        {
            string baudRate;

            Console.Write("Baud Rate(default:{0}): \n", defaultPortBaudRate);
            //baudRate = Console.ReadLine();
            baudRate = "9600";
            if (baudRate == "")
            {
                baudRate = defaultPortBaudRate.ToString();
            }

            return int.Parse(baudRate);
        }

        // Display PortParity values and prompt user to enter a value.
        public static Parity SetPortParity_COM2(Parity defaultPortParity)
        {
            string parity;

            Console.WriteLine("Available Parity options:");
            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Parity value (Default: {0}): \n", defaultPortParity.ToString(), true);
            //parity = Console.ReadLine();
            parity = defaultPortParity.ToString();
            if (parity == "")
            {
                parity = defaultPortParity.ToString();
            }

            return (Parity)Enum.Parse(typeof(Parity), parity, true);
        }
        // Display DataBits values and prompt user to enter a value.
        public static int SetPortDataBits_COM2(int defaultPortDataBits)
        {
            string dataBits;

            Console.Write("Enter DataBits value (Default: {0}): ", defaultPortDataBits);
            //dataBits = Console.ReadLine();
            dataBits = defaultPortDataBits.ToString();
            if (dataBits == "")
            {
                dataBits = defaultPortDataBits.ToString();
            }

            return int.Parse(dataBits.ToUpperInvariant());
        }

        // Display StopBits values and prompt user to enter a value.
        public static StopBits SetPortStopBits_COM2(StopBits defaultPortStopBits)
        {
            string stopBits;

            Console.WriteLine("Available StopBits options:");
            foreach (string s in Enum.GetNames(typeof(StopBits)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter StopBits value (None is not supported and \n" +
             "raises an ArgumentOutOfRangeException. \n (Default: {0}):", defaultPortStopBits.ToString());
            //stopBits = Console.ReadLine();
            stopBits = defaultPortStopBits.ToString();
            if (stopBits == "")
            {
                stopBits = defaultPortStopBits.ToString();
            }

            return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
        }
        public static Handshake SetPortHandshake_COM2(Handshake defaultPortHandshake)
        {
            string handshake;

            Console.WriteLine("Available Handshake options:");
            foreach (string s in Enum.GetNames(typeof(Handshake)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Handshake value (Default: {0}):", defaultPortHandshake.ToString());
            //handshake = Console.ReadLine();
            handshake = defaultPortHandshake.ToString();
            if (handshake == "")
            {
                handshake = defaultPortHandshake.ToString();
            }

            return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
        }
    }

}



