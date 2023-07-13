using System;
using System.Reflection.Emit;
using System.Windows.Forms;
 
namespace MyNamespace
{
    public class MainControl
    {

    }
    public class Tab3
    {

        public static void Tab3Init(TabPage tabPage3, TabControl tabControl)
        {
            // Add some content to each tab
            System.Windows.Forms.Label label = new System.Windows.Forms.Label() { Text = "Control Buttons"};
            Button refreshButton = new Button() { Text = "Refresh", Top = label.Height + 10 }; 
            Button button2 = new Button() { Text = "Button 2", Top = refreshButton.Top + refreshButton.Height + 10 };
            Button button3 = new Button() { Text = "Button 3", Top = button2.Top + button2.Height + 10 };
            Button button4 = new Button() { Text = "Button 4", Top = button3.Top + button3.Height + 10 };

            tabPage3.Controls.Add(label);
            tabPage3.Controls.Add(refreshButton);
            tabPage3.Controls.Add(button2);
            tabPage3.Controls.Add(button3);
            tabPage3.Controls.Add(button4);

            // Button click event handlers
            refreshButton.Click += (sender, e) => Refresh(tabControl);
            button2.Click += Button2_Click;
            button3.Click += Button3_Click;
            button4.Click += Button4_Click;

            // Initialize Tab 3
 
            tabPage3.Controls.Add(refreshButton);
        }

        public static void Refresh(TabControl tabControl)
        {
            // Iterate over each TabPage in the TabControl
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                // Find the ListView control in the current TabPage
                ListView listView = tabPage.Controls.OfType<ListView>().FirstOrDefault();

                // If a ListView control is found, refresh it
                listView?.Refresh();
            }
        }

        private static void Button2_Click(object sender, EventArgs e)
        {
            // Handle the click event for Button2
            MessageBox.Show("Button2 Clicked!");
        }

        private static void Button3_Click(object sender, EventArgs e)
        {
            // Handle the click event for Button2
            MessageBox.Show("Button3 Clicked!");
        }

        private static void Button4_Click(object sender, EventArgs e)
        {
            // Handle the click event for Button2
            MessageBox.Show("Button4 Clicked!");
        }
   
    }

}
