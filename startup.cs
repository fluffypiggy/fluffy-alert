using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using System.ServiceProcess;
using static ListeningPortsHelper;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using GuiNamespace;// Adjust the namespace accordingly
using System.Diagnostics;


public class StartupGroup
{
    private string startupLocation;
    private TabPage tabPage;
    static List<AutoStartApp> autoStartApps = new List<AutoStartApp>();
    ListView listViewAutoStart = new ListView();
    private static Panel containerPanel; // Declare containerPanel as a class-level variable
    string filePath = @"F:\windows\listening_ports.json";

    public StartupGroup(string startupLocation, TabPage tabPage)
    {
        this.startupLocation = startupLocation;
        this.tabPage = tabPage;
    }
    public class AutoStartApp
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Path { get; set; }
        public string Color { get; set; }
    }
    private void getStartupInfo()
    {
        autoStartApps = new List<AutoStartApp>();

        // Retrieve auto-start apps from registry startup group
        using (RegistryKey currentUser = Registry.CurrentUser)
        {
            using (RegistryKey startupKey = currentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (startupKey != null)
                {
                    string[] valueNames = startupKey.GetValueNames();

                    foreach (string valueName in valueNames)
                    {
                        string appPath = (string)startupKey.GetValue(valueName);
                        autoStartApps.Add(new AutoStartApp { Name = valueName, Location = "Registry Startup", Path = appPath });
                    }
                }
            }
        }

        // Retrieve auto-start apps from startup folder
        string startupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Environment.UserName);
        if (Directory.Exists(startupFolderPath))
        {
            string[] startupFiles = Directory.GetFiles(startupFolderPath);
            foreach (string startupFile in startupFiles)
            {
                autoStartApps.Add(new AutoStartApp { Name = Path.GetFileNameWithoutExtension(startupFile), Location = "Startup Folder", Path = startupFile });
            }
        }

        // Retrieve auto-start apps from Task Scheduler
        using (TaskService taskService = new TaskService())
        {
            TaskFolder taskFolder = taskService.RootFolder;
            foreach (Microsoft.Win32.TaskScheduler.Task task in taskFolder.Tasks)
            {
                if (task.Definition.Actions.Count > 0 && task.Definition.Actions[0] is ExecAction execAction)
                {
                    autoStartApps.Add(new AutoStartApp { Name = task.Name, Location = "Task Scheduler", Path = execAction.Path });
                }
            }
        }

        // Retrieve auto-start services
        ServiceController[] services = ServiceController.GetServices();
        foreach (ServiceController service in services)
        {
            using (RegistryKey serviceKey = Registry.LocalMachine.OpenSubKey($"SYSTEM\\CurrentControlSet\\Services\\{service.ServiceName}"))
            {
                if (serviceKey != null)
                {
                    string imagePath = (string)serviceKey.GetValue("ImagePath");
                    autoStartApps.Add(new AutoStartApp { Name = service.ServiceName, Location = "Services", Path = imagePath });
                }
            }
        }



    }


    private void showListView(List<AutoStartApp> combinedList = null)
    {

        var groups = autoStartApps.GroupBy(app => app.Location);
        if (combinedList != null) groups = combinedList.GroupBy(app => app.Location);



        foreach (var group in groups)
        {
            // Create a ListViewGroup for each location
            ListViewGroup listViewGroup = new ListViewGroup(group.Key);
            listViewAutoStart.Groups.Add(listViewGroup);

            foreach (AutoStartApp app in group)
            {
                // Create a ListViewItem for each auto-start app
                ListViewItem listViewItem = new ListViewItem(app.Name);
                listViewItem.SubItems.Add(app.Location);
                listViewItem.SubItems.Add(app.Path);
                listViewItem.Group = listViewGroup;
                // check if color attribute is set 
                if (!string.IsNullOrEmpty(app.Color))
                {
                    if (app.Color.Equals("green", StringComparison.OrdinalIgnoreCase)) 
                        listViewItem.ForeColor = Color.Green; 
                    else if (app.Color.Equals("red", StringComparison.OrdinalIgnoreCase)) 
                        listViewItem.ForeColor = Color.Red; 
                }
                // Add the ListViewItem to the ListView control
                listViewAutoStart.Items.Add(listViewItem);
            }
        }

        // Refresh the ListView to reflect the changes
        listViewAutoStart.Refresh();
    }

    private void createPanel()
    {

        // create panel wrapper to hold checklist and buttons 
        containerPanel = new Panel();
        containerPanel.Dock = DockStyle.Fill;
        // Set the border appearance of the Panel
        containerPanel.BackColor = Color.White; // Set the desired background color
        containerPanel.BorderStyle = BorderStyle.FixedSingle; // Set the border style
        containerPanel.Padding = new Padding(2); // Adjust the padding as needed
        // control buttons -------------------------- 
        Button saveButton = new Button() { Text = "Save"};
        Button refreshButton = new Button() { Text = "Refresh", Top = saveButton.Top + saveButton.Height + 10 }; ;
        Button diffSwitchButton = new Button() { Text = "Diff Switch", Top = refreshButton.Top + refreshButton.Height + 10 }; ;


        int buttonWidth = saveButton.Width;  // Assuming all buttons have the same width
        // Set the left margin of the listViewAutoStart control to start at the right margin of the buttons
        listViewAutoStart.Margin = new Padding(80, 0, 0, 0);
        listViewAutoStart.BorderStyle = BorderStyle.FixedSingle;
        // Create a ListView control
        listViewAutoStart.Dock = DockStyle.Left;
        listViewAutoStart.View = View.Details;

        // Add columns to the ListView 
        listViewAutoStart.Columns.Add("Application", 200);
        listViewAutoStart.Columns.Add("Location", 100);
        listViewAutoStart.Columns.Add("Path", -2); // -2 indicates remaining space

        // Set default column widths
        listViewAutoStart.Columns[0].Width = 200;
        listViewAutoStart.Columns[1].Width = 100;
        listViewAutoStart.Columns[2].Width = -2;

        // Add the ListView, CheckedListBox, and buttons to the container panel
        containerPanel.Controls.Add(saveButton);
        containerPanel.Controls.Add(refreshButton);
        containerPanel.Controls.Add(diffSwitchButton);
        containerPanel.Controls.Add(listViewAutoStart);
        // ************************  add handler 
        saveButton.Click += SaveButton_Click;
        refreshButton.Click += RefreshButton_Click;
        diffSwitchButton.Click += DiffSwitchButton_Click;

        // Subscribe to the DoubleClick event of the ListView
        listViewAutoStart.DoubleClick += ListView_DoubleClick;


        tabPage.Controls.Add(containerPanel);
    }
    public void GetAutoStartApps()
    {
        getStartupInfo(); // populate AutoStartApp List with startup details
        // Clear existing items and groups from the ListView
        tabPage.Controls.Clear();
        createPanel(); // creates panel and buttons and handlers 
        showListView(); // output listview to tabPage;

    }


    // Button click event handlers

    private  void SaveButton_Click(object sender, EventArgs e)
    {
        // Handle the Save button click
        System.Windows.Forms.MessageBox.Show("Replacing previous startup process list");
        Button button = (Button)sender;
        // List<AutoStartApp> autoStartApps = new List<AutoStartApp>();
        // Convert the tuple list to a list of ListeningPort instances
        string json = JsonConvert.SerializeObject(autoStartApps, Formatting.Indented);

        try
        {  
            File.WriteAllText(filePath, json); // Save the JSON to the specified file
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show("An error occurred while writing to the file: " + ex.Message);
        }
    }

    private  void RefreshButton_Click(object sender, EventArgs e)
    {
        // Handle the Refresh button click
        // Clear the ListView
        listViewAutoStart.Items.Clear();
        getStartupInfo(); // populate AutoStartApp List with startup details 
        showListView(); // output listview to tabPage; 
    }

    private  void DiffSwitchButton_Click(object sender, EventArgs e)
    {
        string jsonString = File.ReadAllText(filePath);
        List<AutoStartApp> previousList = JsonConvert.DeserializeObject<List<AutoStartApp>>(jsonString);

        //  -----------------doing the diff here and repopulate listview --------------------------
        // List<AutoStartApp> autoStartApps = new List<AutoStartApp>();
        var listResult = ListDiff.GetDiff(autoStartApps, previousList);
        List<AutoStartApp> matchedList = listResult.matched;
        List<AutoStartApp> inList1List = listResult.inList1;
        List<AutoStartApp> inList2List = listResult.inList2;

        // Set the color for each list item
    //    foreach (AutoStartApp item in matchedList)   item.Color = "black"; 

        foreach (AutoStartApp item in inList1List)   item.Color = "red";
        foreach (AutoStartApp item in inList2List)   item.Color = "green";
        // Combine the lists
        List<AutoStartApp> combinedList = new List<AutoStartApp>();
        combinedList.AddRange(matchedList);
        combinedList.AddRange(inList1List);
        combinedList.AddRange(inList2List);
        showListView(combinedList);
    }

    private void ListView_DoubleClick(object sender, EventArgs e)
    {
        ListView listView = (ListView)sender; // retrieve listview control from sender object

        // Retrieve the clicked item using HitTest
        Point mousePosition = listView.PointToClient(Control.MousePosition);
        ListViewItem clickedItem = listView.GetItemAt(mousePosition.X, mousePosition.Y);

        if (clickedItem != null)
        {
            // Retrieve the full information from the clicked item
            string fullInformation = "";
            string filename =   clickedItem.SubItems[0].Text; // Get the text of the specific subitem

            foreach (ListViewItem.ListViewSubItem subItem in clickedItem.SubItems)
                fullInformation += subItem.Text + Environment.NewLine;
            // Define "Open Bing" button
            Button openBingButton = new Button();
            openBingButton.Text = "Open Bing";
            openBingButton.Click += (s, args) =>
            {
                string encodedFullDetails = Uri.EscapeDataString(filename);
                // Optional: Encode the fullDetails for URL safety
                string url = $"microsoft-edge:https://www.bing.com/search?form=WSBCSL&showconv=1&sendquery=1&q={encodedFullDetails}";
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = url
                };
                Process.Start(psi);

            };

            // Define "ChatGPT" button
            Button chatGptButton = new Button();
            chatGptButton.Text = "ChatGPT";
            chatGptButton.Click += (s, args) =>
            {
                string encodedFullDetails = Uri.EscapeDataString(filename);
                // Optional: Encode the fullDetails for URL safety
                string url = $"https://google.com/search?q={encodedFullDetails}";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = url
                };
                Process.Start(psi);


            };



            // Create a new Form to display the full information
            Form popupForm = new Form();
            popupForm.Text = "Full Information";
            popupForm.Size = new Size(600, 600);

            TextBox textBox = new TextBox();
            textBox.Multiline = true;
            textBox.ReadOnly = true;
            textBox.ScrollBars = ScrollBars.Vertical;
            textBox.Dock = DockStyle.Fill;
            textBox.Text = fullInformation;
            var panelheight = 40;
            // Create a FlowLayoutPanel to hold the controls
            FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
            flowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;
            flowLayoutPanel.Dock = DockStyle.Top;
            flowLayoutPanel.Height = panelheight;
            flowLayoutPanel.Padding = new Padding(0);

            // Create the Font Size selector
            Label fontSizeLabel = new Label();
            fontSizeLabel.Text = "Font Size:";
            ComboBox fontSizeComboBox = new ComboBox();
            fontSizeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            fontSizeComboBox.Items.AddRange(new object[] { "8", "10", "12", "14", "16", "18", "20" });
            fontSizeComboBox.SelectedIndex = 3; // Set default font size to 12
            fontSizeComboBox.SelectedIndexChanged += (s, args) =>
            {
                int selectedFontSize = int.Parse(fontSizeComboBox.SelectedItem.ToString());
                textBox.Font = new Font(textBox.Font.FontFamily, selectedFontSize);
            };


            // Add the controls to the FlowLayoutPanel
            flowLayoutPanel.Controls.Add(fontSizeLabel);
            flowLayoutPanel.Controls.Add(fontSizeComboBox);
            flowLayoutPanel.Controls.Add(openBingButton);
            flowLayoutPanel.Controls.Add(chatGptButton);

            // Create a TableLayoutPanel to hold the TextBox and FlowLayoutPanel
            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, 0);
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, panelheight));
            tableLayoutPanel.Controls.Add(textBox, 0, 1);
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));


            // Add the TableLayoutPanel to the popupForm
            popupForm.Controls.Add(tableLayoutPanel);

            // Show the popupForm
            popupForm.ShowDialog();

        }
    }



}


