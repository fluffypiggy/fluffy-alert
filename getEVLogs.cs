using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Edge;

class EventViewerLogger
{
    private readonly string logName;
    private readonly TabPage tabPage;
    private ListView listView;
    private CheckBox checkBox;

    private SortOrder currentSortOrder = SortOrder.Ascending;
    public EventViewerLogger(string logName, TabPage tabPage)
    {
        this.logName = logName;
        this.tabPage = tabPage;
    }

    public void GetLogsFromLastTwoDays()
    {
        Panel checkBoxPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40
        };

        // Create a CheckBox control
        checkBox = new CheckBox
        {
            Text = "Display in Group ListView",
            Checked = false, // Set the initial checked state as false
            AutoSize = true,
            Location = new System.Drawing.Point(10, 10) // Set the desired location on the form
        };

        // Create a ListView control
        listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            Location = new System.Drawing.Point(0, checkBox.Bottom + 5)
        };
        // Set FullRowSelect property to true
        listView.FullRowSelect = true;

        // Add columns to the ListView
        listView.Columns.Add("Event ID", 80);
        listView.Columns.Add("Entry Type", 80);
        listView.Columns.Add("Source", 80);
        listView.Columns.Add("Message", 400);
        listView.Columns.Add("Time Generated", 150);
        // Create listview
        outputListView();
        // Enable column sorting// Enable sorting 
        // Event handler for column click
        listView.ColumnClick += listViewSort;


        // Subscribe to the DoubleClick event of the ListView
        listView.DoubleClick += ListView_DoubleClick;




        // Clear the existing controls in the TabPage
        tabPage.Controls.Clear();

        // Attach the CheckedChanged event handler
        checkBox.CheckedChanged += CheckBox_CheckedChanged;

        // Add the ListView control to the TabPage
        tabPage.Controls.Add(listView);
        // Add the CheckBox control to the Panel
        checkBoxPanel.Controls.Add(checkBox);

        // Add the Panel to the TabPage
        tabPage.Controls.Add(checkBoxPanel);

    }
    public void outputListView()
    {

        // Create an instance of the EventLog class
        EventLog eventLog = new EventLog(logName);

        // Retrieve the current date and time
        DateTime currentTime = DateTime.Now;

        // Calculate the date and time two days ago
        DateTime twoDaysAgo = currentTime.AddDays(-2);
        // Create a Panel to hold the CheckBox
        // Iterate through each entry in the event log
        var grouplist = "";
        foreach (EventLogEntry entry in eventLog.Entries)
        {
            // Check if the entry falls within the last two days
            if (entry.TimeGenerated >= twoDaysAgo && entry.TimeGenerated <= currentTime)
            {
                // Create a ListViewItem
                ListViewItem item = new ListViewItem(entry.InstanceId.ToString());
                item.SubItems.Add(entry.EntryType.ToString());
                item.SubItems.Add(entry.Source);
                item.SubItems.Add(entry.Message);
                item.SubItems.Add(entry.TimeGenerated.ToString());

                if (checkBox.Checked) // If checkbox is checked, display in group ListView
                {
                    // Find or create a ListViewGroup based on the entry's EntryType
                    //    string groupNames = string.Join(", ", listView.Groups.Cast<ListViewGroup>().Select(group => group.Header));
                    //   Debug.WriteLine("groupname string: " + groupNames);
                    string groupName = entry.EntryType.ToString();

                    ListViewGroup group = new ListViewGroup(groupName);

                    ListViewGroup existingGroup = null;

                    foreach (ListViewGroup groupObj in listView.Groups)
                    {
                        if (groupObj.Header == groupName)
                        {
                            existingGroup = groupObj;
                            break;
                        }
                    }

                    if (existingGroup != null) item.Group = existingGroup;
                    else
                    {
                        listView.Groups.Add(group);
                        item.Group = group;
                    }
                    //Debug.WriteLine("not found: " + groupName);

                }

                // Add the ListViewItem to the ListView
                listView.Items.Add(item);
            }
        }

        // Close the event log
        eventLog.Close();
    }

    private void CheckBox_CheckedChanged(object sender, EventArgs e)
    {
        // Clear the existing ListView items
        listView.Items.Clear();
        // Reload the logs based on the checkbox state
        outputListView();
    }



    public void listViewSort(object sender, ColumnClickEventArgs e)
    {
        // Determine the clicked column
        ColumnHeader clickedColumn = listView.Columns[e.Column];

        // Change the sorting order based on the current order
        if (currentSortOrder == SortOrder.Ascending)
        {
            currentSortOrder = SortOrder.Descending;
            listView.Sorting = SortOrder.Descending;
        }
        else
        {
            currentSortOrder = SortOrder.Ascending;
            listView.Sorting = SortOrder.Ascending;
        }

        // Sort the items based on the clicked column
        listView.ListViewItemSorter = new ListViewItemComparer(e.Column, currentSortOrder);
        listView.Sort();
    }

    // Custom ListViewItemComparer class to perform the sorting
    public class ListViewItemComparer : IComparer
    {
        private int column;
        private SortOrder sortOrder;

        public ListViewItemComparer(int column, SortOrder sortOrder)
        {
            this.column = column;
            this.sortOrder = sortOrder;
        }

        public int Compare(object x, object y)
        {
            ListViewItem item1 = (ListViewItem)x;
            ListViewItem item2 = (ListViewItem)y;

            // Compare the text of the two items in the specified column
            int compareResult = string.Compare(item1.SubItems[column].Text, item2.SubItems[column].Text);

            // Reverse the result if the sort order is descending
            if (sortOrder == SortOrder.Descending)
                compareResult *= -1;

            return compareResult;
        }
    }


    // Event handler for DoubleClick event
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
            foreach (ListViewItem.ListViewSubItem subItem in clickedItem.SubItems)
                fullInformation += subItem.Text + Environment.NewLine;
            // Define "Open Bing" button
            Button openBingButton = new Button();
            openBingButton.Text = "Open Bing";
            openBingButton.Click += (s, args) =>
            { 
                string encodedFullDetails = Uri.EscapeDataString(fullInformation);
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
                string encodedFullDetails = Uri.EscapeDataString(fullInformation);
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
