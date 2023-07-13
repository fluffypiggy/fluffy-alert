 
using System.Net; 
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;
using System.Diagnostics; 
using GuiNamespace; 
using System.Text.Json; 
 
public static class ListeningPortsHelper
{
    public static string displayFilter = "TCP UDP IPv4 IPv6";

    private static Panel containerPanel; // Declare containerPanel as a class-level variable

    public static List<(string Protocol, string AddressType, ushort Port, string Address, int Pid, string ProcessName)> GetListeningPorts()
    {
        List<(string Protocol, string AddressType, ushort Port, string Address, int Pid, string ProcessName)> ports = new List<(string Protocol, string AddressType, ushort Port, string Address, int Pid, string ProcessName)>();

        var tcpTable4 = GetExtendedTcpTable<MIB_TCPTABLE_OWNER_PID>(TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
        var tcpTable6 = GetExtendedTcpTable<MIB_TCP6TABLE_OWNER_PID>(TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, ADDRESS_FAMILY.AF_INET6);

        foreach (var row in tcpTable4.table)
        {
            if (row.dwState == MIB_TCP_STATE.MIB_TCP_STATE_LISTEN)
            {
                string protocol = "TCP";
                string addressType = "IPv4";
                ushort port = (ushort)row.dwLocalPort;
                string address = new IPAddress(BitConverter.GetBytes(row.dwLocalAddr)).ToString();
                int pid = (int)row.dwOwningPid;
                string processName = GetProcessNameByPid(pid);
                ports.Add((protocol, addressType, port, address, pid, processName));
            }
        }

        foreach (var row in tcpTable6.table)
        {
            if (row.dwState == MIB_TCP_STATE.MIB_TCP_STATE_LISTEN)
            {
                string protocol = "TCP";
                string addressType = "IPv6";
                ushort port = (ushort)row.dwLocalPort;
                byte[] addressBytes = new byte[16];
                Array.Copy(row.ucLocalAddr, addressBytes, 16);
                string address = new IPAddress(addressBytes).ToString();
                int pid = (int)row.dwOwningPid;
                string processName = GetProcessNameByPid(pid);
                ports.Add((protocol, addressType, port, address, pid, processName));
            }
        }

        var udpTable4 = GetExtendedUdpTable<MIB_UDPTABLE_OWNER_PID>(UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID);
        var udpTable6 = GetExtendedUdpTable<MIB_UDP6TABLE_OWNER_PID>(UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, ADDRESS_FAMILY.AF_INET6);


        foreach (var row in udpTable4.table)
        {
            string protocol = "UDP";
            string addressType = "IPv4";
            ushort port = (ushort)row.dwLocalPort;
            string address = new IPAddress(BitConverter.GetBytes(row.dwLocalAddr)).ToString();
            int pid = (int)row.dwOwningPid;
            string processName = GetProcessNameByPid(pid);
            ports.Add((protocol, addressType, port, address, pid, processName));

        }

        foreach (var row in udpTable6.table)
        {
            string protocol = "UDP";
            string addressType = "IPv6";
            ushort port = (ushort)row.dwLocalPort;
            byte[] addressBytes = new byte[16];
            Array.Copy(row.ucLocalAddr, addressBytes, 16);
            string address = new IPAddress(addressBytes).ToString();
            int pid = (int)row.dwOwningPid;
            string processName = GetProcessNameByPid(pid);
            ports.Add((protocol, addressType, port, address, pid, processName));

        }

        return ports;
    }
    private static string GetProcessNameByPid(int pid)
    {
        try
        {
            Process process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }
    public static void Init(TabPage tabPage)
    {
        List<(string Protocol, string addressType, ushort Port, string address, int Pid, string ProcessName)> listeningPorts = ListeningPortsHelper.GetListeningPorts();
        // create panel wrapper to hold checklist and buttons 
        containerPanel = new Panel();
        containerPanel.Dock = DockStyle.Fill;
        // Set the border appearance of the Panel
        containerPanel.BackColor = Color.White; // Set the desired background color
        containerPanel.BorderStyle = BorderStyle.FixedSingle; // Set the border style
        containerPanel.Padding = new Padding(2); // Adjust the padding as needed

        // create new list 
        MyListView myListView = new MyListView();
        ListView listView = myListView.GetListView();
        // create checked boxes 

        CheckedListBox checkedListBox = new CheckedListBox();
        checkedListBox.Dock = DockStyle.Left;
     //   checkedListBox.Dock = DockStyle.Top; // Set the DockStyle to Top

        checkedListBox.Width = 80;
        checkedListBox.ItemHeight = 25;
//        checkedListBox.Height = 300;
        // Add the filter keys to the CheckedListBox
        List<string> filterKeys = new List<string>() { "TCP", "UDP", "IPv4","IPv6" };

        checkedListBox.Items.AddRange(filterKeys.ToArray());
        // Set the initial check states for the items
        for (int i = 0; i < checkedListBox.Items.Count; i++)
        {
            checkedListBox.SetItemChecked(i, true);
        }

        // control buttons -------------------------- 
        int checkedListBoxBottom = checkedListBox.Top + checkedListBox.Height;
        Button saveButton = new Button() { Text = "Save", Top = checkedListBoxBottom +  10 };
        Button refreshButton = new Button() { Text = "Refresh", Top = saveButton.Top + saveButton.Height + 10 }; ;
        Button diffSwitchButton = new Button() { Text = "Diff Switch", Top = refreshButton.Top + refreshButton.Height + 10 }; ;

        diffSwitchButton.Tag = listeningPorts; // Assign the listeningPorts list as a custom property
        saveButton.Tag = listeningPorts; // Assign the listeningPorts list as a custom property
        /// ---- end control button ---------------

        // Add the ListView, CheckedListBox, and buttons to the container panel
        containerPanel.Controls.Add(listView);
        containerPanel.Controls.Add(saveButton);
        containerPanel.Controls.Add(refreshButton);
        containerPanel.Controls.Add(diffSwitchButton);
        containerPanel.Controls.Add(checkedListBox);


        // ************************  add handler 
        //  checkedListBox.ItemCheck += CheckedListBox_ItemCheck;
        checkedListBox.ItemCheck += (sender, e) => CheckedListBox_ItemCheck(sender, e, listeningPorts);
        saveButton.Click += SaveButton_Click;
        refreshButton.Click += RefreshButton_Click;
        diffSwitchButton.Click += DiffSwitchButton_Click;

        // Create columns for the ListView
        listView.Columns.Add("Pid", 100);
        listView.Columns.Add("ProcessName", 100);
        listView.Columns.Add("address", 100); 
        listView.Columns.Add("Port", 60);
        listView.Columns.Add("IP4/6", 50);
        listView.Columns.Add("TCP/UDP", 50);

 
        foreach ((string protocol, string addressType, ushort port, string address, int Pid, string ProcessName) in listeningPorts)
        {
            ListViewItem item = new ListViewItem(Pid.ToString());
            item.SubItems.Add(ProcessName);
            item.SubItems.Add(address);
            item.SubItems.Add(port.ToString());
            item.SubItems.Add(addressType);
            item.SubItems.Add(protocol);
            listView.Items.Add(item);
        }


        tabPage.Controls.Add(containerPanel);

    }


    private static void CheckedListBox_ItemCheck(object sender, 
    ItemCheckEventArgs e, List<(string protocol, string addressType, ushort port, string address, int Pid, string ProcessName)> listeningPorts)
{
    CheckedListBox checkedListBox = (CheckedListBox)sender;
    ListView listView = containerPanel.Controls.OfType<ListView>().FirstOrDefault();

        // Get the checked item

        List<string> checkedItems = new List<string>(); 
        foreach (var item in checkedListBox.CheckedItems)
            checkedItems.Add(item.ToString());
        if (e.NewValue == CheckState.Checked)
            checkedItems.Add(checkedListBox.Items[e.Index].ToString());
        else
            checkedItems.Remove(checkedListBox.Items[e.Index].ToString());
        string checkedItem = string.Join(", ", checkedItems.Cast<string>());
     //   string checkedItem = string.Join(", ", checkedListBox.CheckedItems.Cast<string>());

        // checkedItems.ForEach(item => Debug.WriteLine(item));
        listView.Items.Clear();  // Clear existing items
        Debug.WriteLine("--- checked list --------" + checkedItem + ", " + listView.Items.Count);
        displayFilter = checkedItem;
        foreach ((string protocol, string addressType, ushort port, string address, int Pid, string ProcessName) in listeningPorts)
            {
                bool isVisible = checkedItem.Contains(protocol) && checkedItem.Contains(addressType);
                if (isVisible)
                    {
                ListViewItem item = new ListViewItem(Pid.ToString());
                        item.SubItems.Add(ProcessName);
                        item.SubItems.Add(address);
                        item.SubItems.Add(port.ToString());
                        item.SubItems.Add(addressType);
                        item.SubItems.Add(protocol);
                        listView.Items.Add(item);
                }
            }

            listView.Refresh();
            Debug.WriteLine("--- adding row back --------");
 
        }


    // Button click event handlers

    private static void SaveButton_Click(object sender, EventArgs e)
{
    // Handle the Save button click
    System.Windows.Forms.MessageBox.Show("Replacing previous Listening port file: listening_ports.txt");
    Button button = (Button)sender;
    List<(string Protocol, string addressType, ushort Port, string address, int Pid, string ProcessName)> listeningPorts = (List<(string, string, ushort, string, int, string)>)button.Tag;
    // Convert the tuple list to a list of ListeningPort instances
    List<ListeningPort> listeningPortList = listeningPorts.Select(port => new ListeningPort
    {
        Protocol = port.Protocol,
        AddressType = port.addressType,
        Port = port.Port,
        Address = port.address,
        Pid = port.Pid,
        ProcessName = port.ProcessName
    }).ToList();
    string filePath = @"F:\windows\listening_ports.json";
    try
    {
            // Create JsonSerializerOptions with indented formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            // Serialize the listeningPortList to JSON with indentation
            string jsonString = JsonSerializer.Serialize(listeningPortList, options);



            // Save the JSON to the specified file
            File.WriteAllText(filePath, jsonString);
    }
    catch (Exception ex)
    {
        System.Windows.Forms.MessageBox.Show("An error occurred while writing to the file: " + ex.Message);
    }
}

    private static void RefreshButton_Click(object sender, EventArgs e)
    {
        // Handle the Refresh button click

        ListView listView = containerPanel.Controls.OfType<ListView>().FirstOrDefault();
        listView.Items.Clear();  // Clear existing items
        List<(string Protocol, string addressType, ushort Port, string address, int Pid, string ProcessName)> listeningPorts = ListeningPortsHelper.GetListeningPorts();
        foreach ((string protocol, string addressType, ushort port, string address, int Pid, string ProcessName) in listeningPorts)
        {
            bool isVisible = displayFilter.Contains(protocol) && displayFilter.Contains(addressType);
            if (isVisible)
            {
                ListViewItem item = new ListViewItem(Pid.ToString());
                item.SubItems.Add(ProcessName);
                item.SubItems.Add(address);
                item.SubItems.Add(port.ToString());
                item.SubItems.Add(addressType);
                item.SubItems.Add(protocol);
                listView.Items.Add(item);
            }
        }

    }

    private static void DiffSwitchButton_Click(object sender, EventArgs e)
    {
         string filePath = @"F:\windows\listening_ports.json"; 
         string jsonString = File.ReadAllText(filePath);
         List<ListeningPort> deserializedList = JsonSerializer.Deserialize<List<ListeningPort>>(jsonString);

        // Convert the ListeningPort instances back to tuples
        List<(string Protocol, string addressType, ushort Port, string address, int Pid, string ProcessName)> prePorts = deserializedList.Select(port => (
            port.Protocol,
            port.AddressType,
            port.Port,
            port.Address,
            port.Pid,
            port.ProcessName
        )).ToList();


        //  -----------------doing the diff here and repopulate listview --------------------------
        Button button = (Button)sender;
        List<(string Protocol, string addressType, ushort Port, string address, int Pid, string ProcessName)> listeningPorts = (List<(string, string, ushort, string, int, string)>)button.Tag;
        ListView listView = containerPanel.Controls.OfType<ListView>().FirstOrDefault();
        listView.Items.Clear();  // Clear existing items
        // Loop through listeningPorts and compare with prePorts
        foreach (var port in listeningPorts)
        {
            bool foundMatch = false;
            int indexToRemove = -1;

            for (int i = 0; i < prePorts.Count; i++)
            {
                var prePort = prePorts[i];

                if (port.Port == prePort.Port && port.address == prePort.address && port.ProcessName == prePort.ProcessName) // matched if port and address 
                    {
                        foundMatch = true;
                        indexToRemove = i;
                        break;
                    }
            }


            ListViewItem item = new ListViewItem(port.Pid.ToString());
            item.SubItems.Add(port.ProcessName);
            item.SubItems.Add(port.address);
            item.SubItems.Add(port.Port.ToString());
            item.SubItems.Add(port.addressType);
            item.SubItems.Add(port.Protocol); 
            if (!foundMatch)
            {
                item.BackColor = Color.Green;
            }
            else
            {
                var prePort = prePorts[indexToRemove];
                prePorts.RemoveAt(indexToRemove);
                if (port.Pid != prePort.Pid) // matched if port and address 
                {
                    Debug.WriteLine("blue: " + prePort.Pid + " " + port.Pid);
                    item.BackColor = Color.Blue;
                }
            }

            bool isVisible = displayFilter.Contains(port.Protocol) && displayFilter.Contains(port.addressType);
            if (isVisible)
            {
                listView.Items.Add(item);
            }

         }

        // Add the remaining items in prePorts to listView with color red
        foreach (var prePort in prePorts)
        {
            bool isVisible = displayFilter.Contains(prePort.Protocol) && displayFilter.Contains(prePort.addressType);
            if (isVisible)
              {             
                ListViewItem item = new ListViewItem(prePort.Pid.ToString());
                item.SubItems.Add(prePort.ProcessName);
                item.SubItems.Add(prePort.address);
                item.SubItems.Add(prePort.Port.ToString());
                item.SubItems.Add(prePort.addressType);
                item.SubItems.Add(prePort.Protocol);
                item.BackColor = Color.Red;
                listView.Items.Add(item);
                Debug.WriteLine("Red: " + prePort.Pid);
            }

        }
        listView.Refresh();

    }

    // Define a wrapper class for the tuple data
    public class ListeningPort
    {
        public string Protocol { get; set; }
        public string AddressType { get; set; }
        public ushort Port { get; set; }
        public string Address { get; set; }
        public int Pid { get; set; }
        public string ProcessName { get; set; }
    }

}
