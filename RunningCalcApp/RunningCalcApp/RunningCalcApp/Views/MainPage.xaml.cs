using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;
using System.Threading;
using System.IO;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace RunningCalcApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : TabbedPage
    {
        SQLiteConnection conn;
        public bool timerStart = false;
        Stopwatch sw = new Stopwatch();
        TimeSpan prevTime;
        public MainPage()
        {
            InitializeComponent();

            //setting all preferences
            GenderSelection.SelectedItem = Preferences.Get("Gender", "");
            DOBSelection.Date = Preferences.Get("DOB", DateTime.Today);
            var timerPref = Preferences.Get("Time", "00:00:00:0000000");
            Timer.Text = timerPref;
            prevTime = TimeSpan.Parse(timerPref);
            Start.IsEnabled = Preferences.Get("StartBtn", true);
            Stop.IsEnabled = Preferences.Get("StopBtn", true);
            Reset.IsEnabled = Preferences.Get("ResetBtn", true);
            timerStart = Preferences.Get("TimerStarted", false);

            //setting up databases
            string libFolder = FileSystem.AppDataDirectory;
            string fname = System.IO.Path.Combine(libFolder, "Personnel.db");
            conn = new SQLiteConnection(fname);
            conn.CreateTable<Run>();
            conn.CreateTable<MensRecord>();
            conn.CreateTable<WomensRecord>();

            pickerPop();
            populateLog();
            populateTotalMiles();
            LoadMensRecords();
            LoadWomensRecords();

            //continue running the timer if the application was stopped while running
            bool contTimer = Preferences.Get("ContTimer", false);
            if(contTimer)
            {
                timer();
            }
        }//end constructor

        /*Populate the running log that is already in the database*/
        public void populateLog()
        {
            var l1 = from r in conn.Table<Run>()
                                   select r.Date + " " + r.Miles + " miles";
            runLoglv.ItemsSource = l1.Reverse();
        }
        /*calculate total miles run from the log*/
        public void populateTotalMiles()
        {
            int intMiles = 0;
            var miles = from m in conn.Table<Run>()
                        where m.Miles != null
                        select m.Miles;
            foreach(var mi in miles)
            {
                if(Regex.IsMatch(mi, @"^\d+$") && mi != null)
                {
                    intMiles += Convert.ToInt32(mi);
                }
            }
            TotalMiles.Text = intMiles.ToString() + " miles run";
        }

        /*setting gender preference*/
        public void GenderPreference(object sender, EventArgs e)
        {
            string property = (string)GenderSelection.SelectedItem;
            Preferences.Set("Gender", property.ToString());
        }

        /*setting date of birth preference*/
        public void DOBPreference(object sender, EventArgs e)
        {
            var property = DOBSelection.Date;
            Preferences.Set("DOB", property);
            displayAgeGroup();
        }

        /*populating the pickers for hours, mins, secs, on age grade page*/
        public void pickerPop()
        {
            List<int> vals = new List<int>();
            for(int i = 0; i < 60; i++)
            {
                vals.Add(i);
            }

            List<int> hourvals = new List<int>();
            for (int i = 0; i < 13; i++)
            {
                hourvals.Add(i);
            }

            hrP.ItemsSource = hourvals;
            minP.ItemsSource = vals;
            secP.ItemsSource = vals;
        }

        /*Event handler for timer page buttons*/
        public void TimerClicked(object sender, EventArgs e) 
        {
            Button b = (Button)sender;
            if(b.Text.Equals("Start"))
            {
                Preferences.Set("ContTimer", true);
                Start.IsEnabled = false;
                Stop.IsEnabled = true;
                Reset.IsEnabled = false;
                timerStart = true;
                Binding binding = new Binding();
                binding.Mode = BindingMode.TwoWay;
                binding.Source = sw;
                binding.Path = sw.Elapsed.ToString();
                Timer.SetBinding(Label.TextProperty, binding);
                timer();   
            }
            if(b.Text.Equals("Stop"))
            {
                Preferences.Set("ContTimer", false);
                Stop.IsEnabled = false;
                Start.IsEnabled = true;
                Reset.IsEnabled = true;
                timerStart = false;
                sw.Stop();
                Preferences.Set("Time", Timer.Text);
            }
            if(b.Text.Equals("Reset"))
            {
                Preferences.Set("ContTimer", false);
                
                Start.IsEnabled = true;
                Stop.IsEnabled = false;
                Reset.IsEnabled = false;
                timerStart = false;
                sw.Stop();
                Timer.Text = "00:00:00";
                Preferences.Set("Time", Timer.Text);
                prevTime = TimeSpan.Parse(Preferences.Get("Time", Timer.Text));
            }
            //set preferences that were changed
            Preferences.Set("StartBtn", Start.IsEnabled);
            Preferences.Set("StopBtn", Stop.IsEnabled);
            Preferences.Set("ResetBtn", Reset.IsEnabled);
            Preferences.Set("TimerStarted", timerStart);
        }

        /*function called to start timer ever few miliseconds*/
        public void timer()
        {
            sw.Start();
            Device.StartTimer(TimeSpan.FromMilliseconds(1.0), () =>
            {
                Timer.Text = (prevTime + sw.Elapsed).ToString();
                Preferences.Set("Time", Timer.Text);
                return timerStart;
            }); 
        }

        /*adding valid entries to a log*/
        private void AddToLog(object sender, EventArgs e)
        {
            if (LogDistance.Text.Equals("") || (!Regex.IsMatch(LogDistance.Text, @"^\d+$")))
            {
                noMileEntry.Text = "Please enter in a valid distance";
            }
            else
            {
                noMileEntry.Text = "";
                DateTime today = DateTime.Today;
                Run newRun = new Run { Date = today.ToShortDateString(), Miles = LogDistance.Text };
                conn.Insert(newRun);
                populateLog();
                populateTotalMiles();  
            }
        }

        /*url for the credits button*/
        private void creditButton(object sender, EventArgs e)
        {
            Device.OpenUri(new Uri("https://www.miamioh.edu/"));
        }

        /*displaying the age group based off the age*/
        public void displayAgeGroup()
        {
            int ageDisplay = getAge();
            calcAgeGroup(ageDisplay);
        }

        /*displaying the age grade of the person*/
        private void ageGrade(object sender, EventArgs e)
        {
            
            if ((hrP.SelectedIndex >= 0) && (minP.SelectedIndex >= 0) && (secP.SelectedIndex >= 0) && (miles.SelectedIndex >= 0)) {

                displayAgeGroup();
                int age = getAge();
                string ageGroup = calcAgeGroup(age);
                string gender = Preferences.Get("Gender", "");
                TimeSpan personTS = new TimeSpan(Convert.ToInt32(hrP.SelectedItem), Convert.ToInt32(minP.SelectedItem), Convert.ToInt32(secP.SelectedItem));
                if (gender.Equals("Male")) {
                    var time = from data in conn.Table<MensRecord>()
                               where ((data.Distance.Equals(miles.SelectedItem)) && (data.Age.Equals(ageGroup)))
                               select data.Time;
                    string timeToComapare = time.FirstOrDefault();
                    TimeSpan wrTS = convertTime(timeToComapare);
                    if (personTS.Ticks != 0)
                    {
                        double overallGrade = ((Convert.ToDouble(wrTS.Ticks) / Convert.ToDouble(personTS.Ticks)) * 100.0);
                        overallGrade = Math.Round(overallGrade, 2);
                        ageGradeLabel.Text = overallGrade.ToString();
                    }
                    else
                    {
                        ageGradeLabel.Text = "No time entered or no world record available";
                    }
                }
                else if (gender.Equals("Female"))
                {
                    var time = from data in conn.Table<WomensRecord>()
                               where ((data.Distance.Equals(miles.SelectedItem)) && (data.Age.Equals(ageGroup)))
                               select data.Time;
                    string timeToComapare = time.FirstOrDefault();
                    TimeSpan wrTS = convertTime(timeToComapare);
                    if (personTS.Ticks != 0)
                    {
                        double overallGrade = ((Convert.ToDouble(wrTS.Ticks) / Convert.ToDouble(personTS.Ticks)) * 100.0);//wr divided theirs
                        overallGrade = Math.Round(overallGrade, 2);
                        ageGradeLabel.Text = overallGrade.ToString();
                    }
                    else
                    {
                        ageGradeLabel.Text = "No time entered or no world record available";
                    }
                }
            }
        }

        /*converting the time provided by file to timespan*/
        public TimeSpan convertTime(String s) 
        {
            string[] arrString = s.Split(':');
            TimeSpan ts;
            if(arrString.Length == 2)
            {
                string[] mili = arrString[1].Split('.');
                if (mili.Length == 2)
                {
                    ts = new TimeSpan(0, 0, Convert.ToInt32(arrString[0]), Convert.ToInt32(mili[0]), Convert.ToInt32(mili[1]));
                }
                else {
                    ts = new TimeSpan(0, Convert.ToInt32(arrString[0]), Convert.ToInt32(arrString[1]));
                }
            }
            if (arrString.Length == 3)
            {
                ts = new TimeSpan(Convert.ToInt32(arrString[0]), Convert.ToInt32(arrString[1]), Convert.ToInt32(arrString[2]));
            }
            if(arrString.Length == 0)
            {
                //something for na
                return TimeSpan.Zero;
            }
            return ts;
        }

        /*url for more info on age grade*/
        private void ageGradeInfo(object sender, EventArgs e)
        {
            Device.OpenUri(new Uri("https://www.runnersworld.com/advanced/a20801263/age-grade-calculator/"));
        }

        /*calcualting the age group of the user based off of DOB*/
        public string calcAgeGroup(int age)
        {
            int newAge = age - 40;
            int remaining = newAge / 5;
            if (age < 40 || age > 105)
            {
                ageGroup.Text = "Open";
            }
            else
            {
                int display = 40 + (5 * remaining);
                ageGroup.Text = display.ToString();
            }
            return ageGroup.Text;
        }

        /*getting the age of the user*/
        public int getAge()
        {
            DateTime dob = Preferences.Get("DOB", DateTime.Today);
            DateTime now = DateTime.Today;
            if ((now.Month < dob.Month) || ((now.Month == dob.Month) && (now.Day < dob.Day))) {
                return (now.Year - dob.Year) - 1;
            }
            else
            {
                return now.Year - dob.Year;
            }
        }

        /*loading the mens records into the database*/
        public void LoadMensRecords()
        {
            try
            {
                var assembly = IntrospectionExtensions.GetTypeInfo(typeof(MainPage)).Assembly;
                Stream stream = assembly.GetManifestResourceStream("RunningCalcApp.mens.txt");
                StreamReader input = new StreamReader(stream);
                String header = input.ReadLine();
                string[] headers = header.Split(',');

               while (!input.EndOfStream)
                {
                    string line = input.ReadLine();
                    string[] times = line.Split(',');
                    readingMens(headers, times);
                }
            }
            catch (Exception e)
            {
            }
        }

        /*loading womens records into the database*/
        public void LoadWomensRecords()
        {
            try
            {
                var assembly = IntrospectionExtensions.GetTypeInfo(typeof(MainPage)).Assembly;
                Stream stream = assembly.GetManifestResourceStream("RunningCalcApp.womens.txt");
                StreamReader input = new StreamReader(stream);
                String header = input.ReadLine();
                string[] headers = header.Split(',');

                while (!input.EndOfStream)
                {
                    string line = input.ReadLine();
                    string[] times = line.Split(',');
                    readingWomens(headers, times);
                }

            }
            catch (Exception e)
            {
            }
        }

        /*reading in all lines of the mens.txt file*/
        public void readingMens (String[] title, String[] distance)
        {
            for (int i = 1; i < title.Length; i++)
            {
                MensRecord newMR = new MensRecord { Age = title[i], Distance = distance[0], Time = distance[i] };
                conn.Insert(newMR);
            }
        }

        /*reading in all lines of the womens.txt file*/
        public void readingWomens(String[] title, String[] distance)
        {
            for (int i = 1; i < title.Length; i++)
            {
                WomensRecord newWR = new WomensRecord { Age = title[i], Distance = distance[0], Time = distance[i] };
                conn.Insert(newWR);
            }
        }
    }
}