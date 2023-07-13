using MyNamespace;
using System;
using System.Diagnostics; 
 using System.Security.Principal;
using System.Text;

namespace TabbedWindowExample
{
    // Define the TextBoxWriter class
    public partial class MainForm : Form
    {
        public static string FilePath = Application.StartupPath; // default path
        public static TabPage OutputTabPage { get; private set; } // output tab page for console output
        public static TextBox OutputTextBox { get; private set; }
         
        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load; // Wire up the event handler 
            // this sets up a form 
            // same a Form ... new Form
            this.Text = "  Fluffy Alert";
            this.Size = new Size(800, 600); // Set the default size to 800x600 pixels

        }
        private void InitializeComponent()
        {
            // Initialize the form's components and controls
            this.SuspendLayout();
            // ...
            // Add your component initialization code here
            // ...

            this.ResumeLayout(false);
        }
 

        private void MainForm_Load(object? sender, EventArgs e)
        { 

            // Create the tabs
            TabControl tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            // Add the tab control to the main form
            Controls.Add(tabControl);

            // Create the tabpages
            TabPage tabPage1 = new TabPage("Processes");
            TabPage tabPage2 = new TabPage("Listening Ports");
            TabPage tabPage3 = new TabPage("Tab 3");
            TabPage tabPage4 = new TabPage("Event Logs");
            TabPage tabPage5 = new TabPage("Startup");
            TabPage tabPage6 = new TabPage("Services");


            // [TabPage outputTabPage] -------- setup tab page for output --------------
            //    TabPage outputTabPage = new TabPage("Output");
            OutputTabPage = new TabPage("Output"); // utilize global static 
            OutputTextBox = new TextBox();
            OutputTextBox.Multiline = true;
            OutputTextBox.Dock = DockStyle.Fill;
            OutputTabPage.Controls.Add(OutputTextBox);
            tabControl.TabPages.Add(OutputTabPage);

            // [TabPage TabPage3]  -- Create a StringWriter to capture the output  ----------------------------
            Tab3.Tab3Init(tabPage3, tabControl);
            // ------------------------------------------------------------
            StringWriter stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            Console.WriteLine("This will be redirected to the outputTabPage");
            string capturedOutput = stringWriter.ToString();
            OutputTextBox.Text = capturedOutput;
            //---------------------------------------------------------------------

            // [TabPage TabPage1]  -- does Processes  ----------------------------
            MeasureExecutionTime(() => ProcessUtils.DumpProcessListToTab(tabPage1));
            // [TabPage TabPage2]  -- does Listening Ports  ----------------------------
            ListeningPortsHelper.Init(tabPage2);
            // [TabPage TabPage4] >>>>>  load event viewer logs ----------------------------
            EventViewerLogger logger = new EventViewerLogger("Application", tabPage4);
            logger.GetLogsFromLastTwoDays(); // getEVLogs.cs
            // [TabPage TabPage5] >>>>>  load event viewer logs ----------------------------
            // Create an instance of the StartupGroup class
            StartupGroup startupGroup = new StartupGroup("Startup", tabPage5);
            startupGroup.GetAutoStartApps();
            // Add the tabs to the tab control
            tabControl.TabPages.Add(tabPage1);
            tabControl.TabPages.Add(tabPage2);
            tabControl.TabPages.Add(tabPage3);
            tabControl.TabPages.Add(tabPage4);
            tabControl.TabPages.Add(tabPage5);
            tabControl.TabPages.Add(tabPage6);

        }
        private static void MeasureExecutionTime(Action action)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            action.Invoke();

            stopwatch.Stop();

            Console.WriteLine($"Execution time: {stopwatch.Elapsed.TotalMilliseconds} ms");
           // MessageBox.Show($"Execution time: {stopwatch.Elapsed.TotalMilliseconds} ms");

        }
        [STAThread]
        private static Mutex statCheck()
        {

            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            // admin prompt
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                // User has administrative privileges
                Application.EnableVisualStyles();
                Application.Run(new MainForm());
            }
            else
            {
                // User does not have administrative privileges
                DialogResult result = MessageBox.Show("This program requires administrative privileges to run.\n\n" +
                                                      "Do you want to continue?",
                                                      "Privilege Elevation Required",
                                                      MessageBoxButtons.YesNo,
                                                      MessageBoxIcon.Error);
                if (result == DialogResult.Yes)
                {
                    // Continue execution
                    Application.EnableVisualStyles();
                    Application.Run(new MainForm());
                }
                else
                {
                    // Exit the program
                    Environment.Exit(0);
                }
            }

            // Specify a unique name for the mutex
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            string mutexName = "fluffyMonitor";
            //  string filename  = Path.Combine(FilePath,"listening_ports.txt");
            Environment.CurrentDirectory = Application.StartupPath;
            // Check if another instance of the application is already running
            bool isAnotherInstanceRunning;
            Mutex mutex = new Mutex(true, mutexName, out isAnotherInstanceRunning);
            if (isAnotherInstanceRunning)
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    try
                    {
                        Mutex.OpenExisting(mutexName).Close();
                    }
                    catch (WaitHandleCannotBeOpenedException)
                    {
                        // Mutex does not exist or cannot be opened by the process
                        continue;
                    }

                    // Mutex is opened successfully by the process
                    // Get the PID of the owning process
                    int processId = process.Id;
                }
            }

            // No other instance is running, continue with your application logic
            return mutex;
        }
        static void Main()
        {
//           Application.SetCompatibleTextRenderingDefault(false);

            Mutex mutex  =  statCheck();  // get a mutex object 
            Debug.WriteLine("mutex is : " + mutex.ToString());

            // Add your code here to create and display the form, add controls, etc.

            Application.EnableVisualStyles();

            // When your application is finished, release the mutex
             mutex.ReleaseMutex();
            mutex.Close(); // Close the mutex object


        }

    }
}

