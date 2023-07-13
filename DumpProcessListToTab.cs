using System.Diagnostics;
using System.Management; 
using GuiNamespace;
using System.Text.Json; 

namespace MyNamespace
{
    public static class ProcessUtils
    {
        private static Panel containerPanel; // Declare containerPanel as a class-level variable
        public static string displayFilter = "";

        // Custom data structure to represent the hierarchical relationship
        public class ProcessNode
        {
            public string pid { get; set; }
            public string Name { get; set; }
            public string ppid { get; set; }
            public string pname { get; set; }
            public string WorkingSetSize { get; set; }
            public string Path { get; set; }
            public string CommandLine { get; set; }
        }
        public static void DumpProcessListToTab(TabPage tabPage)
        {

            // >>> create panel wrapper to hold checklist and buttons 
            containerPanel = new Panel();
            containerPanel.Dock = DockStyle.Fill;
            // Set the border appearance of the Panel
            containerPanel.BackColor = Color.White; // Set the desired background color
            containerPanel.BorderStyle = BorderStyle.FixedSingle; // Set the border style
            containerPanel.Padding = new Padding(2); // Adjust the padding as needed
            // <<<<---------------------------------------------------


            // >>>> create checkedlistbox -------------------------------------
            CheckedListBox checkedListBox = new CheckedListBox();
            checkedListBox.Dock = DockStyle.Left;
            //   checkedListBox.Dock = DockStyle.Top; // Set the DockStyle to Top
            checkedListBox.Width = 80;
            checkedListBox.ItemHeight = 25;
            //        checkedListBox.Height = 300;
            // Add the filter keys to the CheckedListBox
            List<string> filterKeys = new List<string>() { "Child", "Unique" };

            checkedListBox.Items.AddRange(filterKeys.ToArray());
            // Set the initial check states for the items
            for (int i = 0; i < checkedListBox.Items.Count; i++)
            {
                checkedListBox.SetItemChecked(i, true);
            }

            // control buttons -------------------------- 
            int checkedListBoxBottom = checkedListBox.Top + checkedListBox.Height;
            Button saveButton = new Button() { Text = "Save", Top = checkedListBoxBottom + 10 };
            Button refreshButton = new Button() { Text = "Refresh", Top = saveButton.Top + saveButton.Height + 10 }; ;
            Button diffSwitchButton = new Button() { Text = "Diff Switch", Top = refreshButton.Top + refreshButton.Height + 10 }; ;

            /// ---- end control button ---------------

            //---------------------------------------------------------------------------------------------
            // construct list view 
            MyListView myListView = new MyListView();
            ListView listView = myListView.GetListView();
            // Create columns for the ListView
            listView.Columns.Add("PID", 80);
            listView.Columns.Add("Name", 200);
            listView.Columns.Add("PPID", 80);
            listView.Columns.Add("PPName", 200);
            listView.Columns.Add("WorkingSetSizeh", 10);
            listView.Columns.Add("Path", 300);
            listView.Columns.Add("CommandLine", 300);

            // Add the ListView, CheckedListBox, and buttons to the container panel
            containerPanel.Controls.Add(listView);
            containerPanel.Controls.Add(saveButton);
            containerPanel.Controls.Add(refreshButton);
            containerPanel.Controls.Add(diffSwitchButton);
            containerPanel.Controls.Add(checkedListBox);
            tabPage.Controls.Add(containerPanel);
            // >>> populate listview 
            // BuildList(searcher, listView);
            ListObj mylistobj = new ListObj();
            mylistobj.CreateListView(listView);
            // ************************  add handler 
            //  checkedListBox.ItemCheck += CheckedListBox_ItemCheck;
            checkedListBox.ItemCheck += (sender, e) => mylistobj.CheckedListBox_ItemCheck(sender, e);
            saveButton.Click += mylistobj.SaveButton_Click;
            refreshButton.Click += mylistobj.RefreshButton_Click;
            diffSwitchButton.Click += mylistobj.DiffSwitchButton_Click;
            diffSwitchButton.Tag = listView; // Assign the listeningPorts list as a custom property
            saveButton.Tag = listView; // Assign the listeningPorts list as a custom property
            refreshButton.Tag = listView; // Assign the listeningPorts list as a custom property


        }

        private class ListObj
        {
            public ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process");
            private ProcessNode myPnode = new ProcessNode();

            public Dictionary<int, List<int>> ppidTree = new Dictionary<int, List<int>>();
            public Dictionary<int, ProcessNode> processDictionary = new Dictionary<int, ProcessNode>();
            string filterkey = "Unique Child";
            public ListObj()
            {
                // build two dictionary
                buildDictionary();
                Debug.WriteLine("finished building data");

            }
            private void buildDictionary()
            {
                foreach (ManagementObject process in searcher.Get())
                {
                    string PPName ="";
                    string processId = process["ProcessId"]?.ToString() ?? string.Empty;
                    string processName = process["Name"]?.ToString() ?? string.Empty;
                    string parentProcessId = process["ParentProcessId"]?.ToString() ?? string.Empty;
                    string workingSetSize = process["WorkingSetSize"]?.ToString() ?? string.Empty;
                    string executablePath = process["ExecutablePath"]?.ToString() ?? string.Empty;
                    string commandLine = process["CommandLine"]?.ToString() ?? string.Empty;

                    int pid = int.Parse(processId);
                    int ppid = int.Parse(parentProcessId);
                    PPName = processDictionary.ContainsKey(ppid) ? processDictionary[ppid]?.Name : "";

                    Debug.WriteLine("build: " + processId + processName);
                    ProcessNode node = new ProcessNode
                    {
                        pid = processId,
                        Name = processName,
                        ppid = parentProcessId,
                        pname = PPName,
                        WorkingSetSize = workingSetSize,
                        Path = executablePath,
                        CommandLine = commandLine,
                    };
                    // Add the ProcessNode to the processDictionary using PID as the index
                    processDictionary[pid] = node;
                }
                ppidTree = processDictionary.GroupBy(entry => int.Parse(entry.Value.ppid))
    .ToDictionary(group => group.Key, group => group.Select(entry => int.Parse(entry.Value.pid)).ToList());

            }
            public void BuildimageList(Dictionary<int, List<int>> ppidTree, Dictionary<int, ProcessNode> processDictionary, ListView listView)
            {
                listView.View = View.Details;
                // Create the ListViewGroups for each PPID
                var sortedPpidTree = ppidTree.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

                var keysInValues = new HashSet<int>(sortedPpidTree.SelectMany(entry => entry.Value));
                //   var uniqueKeys = sortedPpidTree.Keys.Where(key => !keysInValues.Contains(key)).ToList();
                //   uniqueKeys.ForEach(key => Debug.WriteLine(key));
                // create dead parent 

                foreach (var entry in ppidTree)
                {
                    int ppid = entry.Key;

                    List<int> childList = ppidTree[ppid];

                    // Get the parent ProcessNode from the processDictionary
                    // ...
                    if (!processDictionary.ContainsKey(ppid))
                    {
                        Debug.WriteLine("PPID " + ppid + " is a dead parent. Skipping to the next.");

                        ProcessNode emptynode = new ProcessNode
                        {
                            pid = ppid.ToString(),
                            Name = "Dead Proc",
                            ppid = "",
                            pname = "Dead Proc",
                            WorkingSetSize = "",
                            Path = "",
                            CommandLine = "",
                        };
                        // Add the ProcessNode to the processDictionary using PID as the index
                        processDictionary[ppid] = emptynode;
                    }
                    ProcessNode Node = processDictionary[ppid];
                    createImageList();
                    void createImageList()
                    {

                   
                    // Create an ImageList
                    ImageList imageList = new ImageList();

                    // Add the plus and minus icons to the ImageList

                    // Load the icon files
                    Icon minusIcon = new Icon("minus_icon.ico");
                    Icon addIcon = new Icon("add_icon.ico");

                    imageList.Images.Add("Plus", addIcon);   // Plus sign icon
                    imageList.Images.Add("Minus", minusIcon); // Minus sign icon
                    listView.SmallImageList = imageList;
                    foreach (var imageKey in imageList.Images.Keys)
                    {
                        Image image = imageList.Images[imageKey];
                        // Print or perform actions with the image
                        Console.WriteLine("Image Key: " + imageKey);
                        Console.WriteLine("Image Size: " + image.Size);
                        // ...
                    }
                    }
                    addOneListViewItem(ppid); // adds ppid to listview 

                    void addOneListViewItem(int pid)
                    {
                        ListViewItem listItem = new ListViewItem(pid.ToString());
                        if (ppidTree.ContainsKey(pid) && ppidTree[pid].Count > 0)
                        {
                            listItem.ImageKey = "Plus";     // Plus sign icon for the first column of Item 1
                        }
                        else
                        {
                            listItem.ImageKey = "";     // Plus sign icon for the first column of Item 1
                        }
                        listItem.SubItems.Add(Node.Name);
                        listItem.SubItems.Add(Node.ppid);
                        listItem.SubItems.Add(Node.pname);
                        listItem.SubItems.Add(Node.WorkingSetSize);
                        listItem.SubItems.Add(Node.Path);
                        listItem.SubItems.Add(Node.CommandLine);
                        // Check if the item has children to determine the expand/collapse state
                        listView.Items.Add(listItem);
                    }


                    // build list view
                    foreach (int pid in childList)
                    {
                        // Example code within the loop
                        ListViewItem listItem = new ListViewItem(pid.ToString());
                        addOneListViewItem(pid); // adds ppid to listview 
                    }
                }

                // event handler
                listView.MouseClick += (sender, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        ListViewItem clickedItem = listView.GetItemAt(e.X, e.Y);

                        if (clickedItem != null)
                        {
                            int iconIndex = clickedItem.ImageIndex;
                            string imageKey = clickedItem.ImageKey; // Retrieve the ImageKey value

                            Debug.WriteLine("mouse clicked icon index is : " + imageKey + iconIndex);

                            string pid = clickedItem.Text;
                            string plusminus = clickedItem.SubItems[0].Text;
                            Debug.WriteLine("pid : " + pid, plusminus);

                            // string secondSubItemText = clickedItem.SubItems[1].Text;
                            // string thirdSubItemText = clickedItem.SubItems[2].Text;

                            if (imageKey == "Plus")
                            {
                                clickedItem.ImageKey = "Minus"; // Plus sign icon for the first column of Item 1
                                AddChildItems(clickedItem);
                            }
                            else if (imageKey == "Minus")
                            {
                                clickedItem.ImageKey = "Plus"; // Plus sign icon for the first column of Item 1
                                RemoveChildItems(clickedItem);

                            }
                        }
                    }
                };

                // Method to remove child items of a collapsed parent item
                void RemoveChildItems(ListViewItem parentItem)
                {
                    int parentIndex = parentItem.Index;
                    int childIndex = parentIndex + 1;
                    int ppid = int.Parse(parentItem.Text);
                    int num_child = ppidTree[ppid].Count + childIndex;
                    Debug.WriteLine("num_child childIndex listView.Items.Count");

                    while (childIndex < num_child)  // remove up to child 
                    {
                        Debug.WriteLine(num_child + childIndex + listView.Items.Count );
                        ListViewItem childItem = listView.Items[childIndex];
                        listView.Items.RemoveAt(childIndex);
                        childIndex++;
                    }
                }

                // Method to add child items of an expanded parent item
                void AddChildItems(ListViewItem parentItem)
                {
                    int parentIndex = parentItem.Index;
                    int childIndex = parentIndex + 1;
                    int ppid = int.Parse(parentItem.Text);
                    int num_child = ppidTree[ppid].Count;
                    List<int> childs = ppidTree[ppid];

                    foreach (int pid in childs)
                    {
                        ProcessNode Node = processDictionary[pid];
                        // Example code within the loop
                        ListViewItem listItem = new ListViewItem(pid.ToString());
                        // Check if the item has children to determine the expand/collapse state
                        if (ppidTree.ContainsKey(pid) && ppidTree[pid].Count > 0)
                        {
                            listItem.ImageKey = "Plus"; // Plus sign icon for the first column of Item 1
                        }
                        else
                        {
                            listItem.ImageKey = ""; // Plus sign icon for the first column of Item 1
                        }
                        listItem.SubItems.Add(Node.Name);
                        listItem.SubItems.Add(Node.ppid);
                        listItem.SubItems.Add(Node.pname);
                        listItem.SubItems.Add(Node.WorkingSetSize);
                        listItem.SubItems.Add(Node.Path);
                        listItem.SubItems.Add(Node.CommandLine);
                        listView.Items.Insert(childIndex, listItem);
                        childIndex++;
                    }
                }

            }
           
            
            
            public void plainList(ListView listView, string filterkey)
            {
                // filterkey values 
                // Unique -- only kept one process name 
                // Child -- 
                List<int> keysList = processDictionary.Select(entry => entry.Key).ToList();
                Debug.WriteLine("filterkey =" + filterkey);


                if (! filterkey.Contains("Child"))
                {
                    keysList = processDictionary
                        .GroupBy(entry => entry.Value.pname)
                        .Select(group => int.Parse(group.First().Value.ppid))
                        .ToList();
                    Debug.WriteLine("filterkey contains child");

                }
                else if (filterkey.Contains("Unique"))
                {
                    keysList = processDictionary
                        .GroupBy(entry => entry.Value.Name)
                        .Select(group => int.Parse(group.First().Value.pid))
                        .ToList();

                }
                else {
                    keysList = processDictionary.Select(entry => entry.Key).ToList();
                }

                plainListView(keysList);
                void plainListView(List <int> keysList)
                {
                    foreach (int entry in keysList)
                    {
                        int pid = entry;
                        ProcessNode Node = processDictionary[pid];
                        ListViewItem listItem = new ListViewItem(pid.ToString());
                        listItem.SubItems.Add(Node.Name);
                        listItem.SubItems.Add(Node.ppid);
                        listItem.SubItems.Add(Node.pname);
                        listItem.SubItems.Add(Node.WorkingSetSize);
                        listItem.SubItems.Add(Node.Path);
                        listItem.SubItems.Add(Node.CommandLine);
                        // Check if the item has children to determine the expand/collapse state
                        listView.Items.Add(listItem);
                    }
                }
 

            }
            // return a listview 
            public ListView CreateListView(ListView listView)
            {
                Debug.WriteLine("--- creating listview --------");

                //BuildimageList(ppidTree, processDictionary, listView);
               // BuildListViewGroup(ppidTree, processDictionary, listView);
                plainList(listView, filterkey);
                Debug.WriteLine("--- Refresh listview --------");
                listView.Refresh();
                return listView;
            }


            public void SaveButton_Click(object sender, EventArgs e)
            {
                // Handle the Save button click
                System.Windows.Forms.MessageBox.Show("Replacing previous process list file: process_list.txt");
                Button button = (Button)sender;
                string filePath = @"F:\windows\process_list.json";
                try
                {
                    // Create JsonSerializerOptions with indented formatting
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };

                    // Serialize the listeningPortList to JSON with indentation
                    string jsonString = JsonSerializer.Serialize(processDictionary);



                    // Save the JSON to the specified file
                    File.WriteAllText(filePath, jsonString);
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("An error occurred while writing to the file: " + ex.Message);
                }
            }

            public void RefreshButton_Click(object sender, EventArgs e)
            {
                Button refreshButton = (Button)sender; // Assuming the sender is a Button control
                ListView listView = (ListView)refreshButton.Tag; // Assuming the ListView control is stored in the Tag property of the Button
                // Access the ListView control and perform actions
                listView.Items.Clear(); // Example: Clear the items in the ListView
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process");
                ppidTree.Clear();  // Clears all key-value pairs from the ppidTree dictionary
                processDictionary.Clear();  // Clears all key-value pairs from the processDictionary dictionary
                buildDictionary();
               // BuildimageList(ppidTree, processDictionary, listView);
             //   BuildListViewGroup(ppidTree, processDictionary, listView);
                plainList(listView, filterkey);

                Debug.WriteLine("refresh button clicked:"); // Handle the Refresh button click
            }

            public  void DiffSwitchButton_Click(object sender, EventArgs e)
            {
                string filePath = @"F:\windows\process_list.json";
                string jsonString = File.ReadAllText(filePath);
                Button button = (Button)sender;
                ListView listView = containerPanel.Controls.OfType<ListView>().FirstOrDefault();
                listView.Items.Clear();
                Dictionary<int, ProcessNode> deserializedList=JsonSerializer.Deserialize<Dictionary<int, ProcessNode>>(jsonString);
                //  -----------------doing the diff here and repopulate listview --------------------------
                // Get a unique list of names indexed by pid
                List<KeyValuePair<int, string>> uniqueNamesByPid = processDictionary
                    .GroupBy(entry => entry.Value.Name)
                    .Select(group => new KeyValuePair<int, string>(group.Select(entry => entry.Key).FirstOrDefault(), group.Key))
                    .ToList();

                List<string> uniqueNames = deserializedList.Select(node => node.Value.Name).Distinct().ToList();
                Dictionary<string, int> name2pids = deserializedList
                    .GroupBy(entry => entry.Value.Name)
                    .ToDictionary(group => group.Key, group => int.Parse(group.Select(entry => entry.Value.pid).FirstOrDefault()));
     

                foreach (var kvp in uniqueNamesByPid)
                {
                    int pid =  kvp.Key;
                    string name = kvp.Value;
                    ProcessNode Node = processDictionary[pid];
                    ListViewItem listItem = additem(pid);
 
                    if (uniqueNames.Contains(name))   uniqueNames.Remove(name);
                    else     listItem.BackColor = Color.Green;
                    listView.Items.Add(listItem);

                }

                // Add the remaining items in prePorts to listView with color red
                
                foreach (string name in uniqueNames)
                {
                    int pid = name2pids[name];
                    Debug.WriteLine("red :" + pid + name);
                    ListViewItem listItem = additem(pid);
                    listItem.BackColor = Color.Red;
                    listView.Items.Add(listItem);
                }
                listView.Refresh();
                ListViewItem  additem(int pid){
                    ListViewItem listItem = new ListViewItem(pid.ToString());
                    ProcessNode Node = deserializedList[pid];
                    listItem.SubItems.Add(Node.Name);
                    listItem.SubItems.Add(Node.ppid);
                    listItem.SubItems.Add(Node.pname);
                    listItem.SubItems.Add(Node.WorkingSetSize);
                    listItem.SubItems.Add(Node.Path);
                    listItem.SubItems.Add(Node.CommandLine);
                    return listItem;
                }


            }

            public void CheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
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
                filterkey = checkedItem;
                plainList(listView, filterkey);
                listView.Refresh();
                Debug.WriteLine("--- adding row back --------");

            }

            public void BuildListViewGroup(Dictionary<int, List<int>> ppidTree, Dictionary<int, ProcessNode> processDictionary, ListView listView)
            {
                listView.View = View.Details;
                foreach (var entry in ppidTree)
                {
                    int ppid = entry.Key;
                    List<int> childList = entry.Value;

                    Debug.WriteLine("PPID: " + ppid);
                    Debug.WriteLine("Child List: ");
                    foreach (int pid in childList)
                    {
                        Debug.WriteLine(" - " + pid);
                    }
                    Debug.WriteLine("------------------");
                }
                // Create the ListViewGroups for each PPID
                var sortedPpidTree = ppidTree.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

                var keysInValues = new HashSet<int>(sortedPpidTree.SelectMany(entry => entry.Value));
                var uniqueKeys = sortedPpidTree.Keys.Where(key => !keysInValues.Contains(key)).ToList();
                Debug.WriteLine("build unique list of ppid ");
                uniqueKeys.ForEach(key => Debug.WriteLine(key));

                foreach (var entry in ppidTree)
                {
                    int ppid = entry.Key;

                    List<int> childList = ppidTree[ppid];

                    // Get the parent ProcessNode from the processDictionary
                    // ...
                    if (!processDictionary.ContainsKey(ppid))
                    {
                        Debug.WriteLine("PPID " + ppid + " is a dead parent. Skipping to the next.");

                        ProcessNode emptynode = new ProcessNode
                        {
                            pid = ppid.ToString(),
                            Name = "Dead Proc",
                            ppid = "",
                            pname = "Dead Proc",
                            WorkingSetSize = "",
                            Path = "",
                            CommandLine = "",
                        };
                        // Add the ProcessNode to the processDictionary using PID as the index
                        processDictionary[ppid] = emptynode;
                    }

                    ProcessNode Node = processDictionary[ppid];

                    ListViewGroup group = new ListViewGroup(ppid.ToString()); // Create a ListViewGroup for each PPID
                    listView.Groups.Add(group); // Add the group to the ListView

                    addOneListViewItem(ppid); // adds ppid to ListView

                    void addOneListViewItem(int pid)
                    {
                        ListViewItem listItem = new ListViewItem(pid.ToString());
                        listItem.SubItems.Add(Node.Name);
                        listItem.SubItems.Add(Node.ppid);
                        listItem.SubItems.Add(Node.pname);
                        listItem.SubItems.Add(Node.WorkingSetSize);
                        listItem.SubItems.Add(Node.Path);
                        listItem.SubItems.Add(Node.CommandLine);
                        listItem.Group = group; // Set the group for the ListViewItem
                        listView.Items.Add(listItem);
                    }

                    // Build list view
                    foreach (int pid in childList)
                    {
                        // Example code within the loop
                        addOneListViewItem(pid); // adds ppid to ListView
                    }
                }
            }


        }





    }





}
