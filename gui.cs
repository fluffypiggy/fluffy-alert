using System.Collections;

namespace GuiNamespace
{
    public class MyListView
    {
        private ListView listView;          // type declaration

        public MyListView()
        {

            listView = new ListView();

            listView.View = View.Details;
            listView.Dock = DockStyle.Fill;
            listView.ListViewItemSorter = new ListViewItemComparer();
            listView.ColumnClick += ListView_ColumnClick;
        }
        private static void ListView_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            ListView? listView = sender as ListView;
            if (listView is null)
            {
                MessageBox.Show("gui.cs 33 - Listview is null ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Proceed with further operations using the listView object

            ListViewItemComparer sorter = (ListViewItemComparer)listView.ListViewItemSorter;

            // Determine if the clicked column is already the column that is being sorted
            if (e.Column == sorter.SortColumn)
            {
                // Reverse the current sort direction for this column
                sorter.SortOrder = (sorter.SortOrder == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending
                sorter.SortColumn = e.Column;
                sorter.SortOrder = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options
            listView.Sort();
        }
        public ListView GetListView()
        {
            return listView;
        }
    }

    public class ListViewItemComparer : IComparer
    {
        public int SortColumn { get; set; }
        public SortOrder SortOrder { get; set; }

        public int Compare(object? x, object? y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;

            string textX = itemX.SubItems[SortColumn].Text;
            string textY = itemY.SubItems[SortColumn].Text;

            int result;
            if (int.TryParse(textX, out int valueX) && int.TryParse(textY, out int valueY))
            {
                // Compare as integers
                result = valueX.CompareTo(valueY);
            }
            else
            {
                // Compare as strings
                result = string.Compare(textX, textY);
            }

            // Invert the result if the sort order is descending
            if (SortOrder == SortOrder.Descending)
            {
                result = -result;
            }

            return result;
        }
    }

    public static class ListDiff
    {

        public static (List<T> matched, List<T> inList1, List<T> inList2) GetDiff<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            List<T> matched = new List<T>();
            List<T> inList1 = new List<T>();
            List<T> inList2 = new List<T>();

            // Convert IEnumerable to List for efficient indexing (if needed)
            List<T> list1Items = list1 as List<T> ?? list1.ToList();
            List<T> list2Items = list2 as List<T> ?? list2.ToList();

            foreach (T item1 in list1Items)
            {
                bool found = false;

                foreach (T item2 in list2Items)
                {
                    if (EqualityComparer<T>.Default.Equals(item1, item2))
                    {
                        found = true;
                        matched.Add(item1);
                        list2Items.Remove(item2); // Remove matched item from list2
                        break;
                    }
                }

                if (!found)
                {
                    inList1.Add(item1);
                }
            }

            inList2.AddRange(list2Items); // Remaining items in list2 are inList2

            return (matched, inList1, inList2);
        }


    }


}
