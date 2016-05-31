﻿using Alexa;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace ModelCheck
{

    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.clearColumns();
            this.hostText.Text = ModelCheck.Default.Host;
            this.portText.Text = ModelCheck.Default.Port;
            this.accessToken.Text = ModelCheck.Default.AccessToken;
        }

        private void clearColumns()
        {
            listView.Clear();
            listView.Columns.Clear();
            listView.Columns.Add("applianceId", -1, HorizontalAlignment.Left);
            listView.Columns.Add("manufacturerName", -1, HorizontalAlignment.Left);
            listView.Columns.Add("modelName", -1, HorizontalAlignment.Left);
            listView.Columns.Add("version", -1, HorizontalAlignment.Left);
            listView.Columns.Add("friendlyName", -1, HorizontalAlignment.Left);
            listView.Columns.Add("friendlyDescription", -1, HorizontalAlignment.Left);
            listView.Columns.Add("isReachable", -1, HorizontalAlignment.Left);
            listView.Columns.Add("dimmable", -1, HorizontalAlignment.Left);
            listView.Columns.Add("path", -1, HorizontalAlignment.Left);
            this.statusText.Text = string.Format("Item Count:{0}", listView.Items.Count);
        }

        private void queryButton_Click(object sender, EventArgs e)
        {
            Alexa.SmartHome.V1.DiscoveryRequest request = new Alexa.SmartHome.V1.DiscoveryRequest();
            request.payload.accessToken = accessToken.Text;
            string data = JsonConvert.SerializeObject(request);

            var dataToDeserialize = jsonData(string.Format(@"https://{0}:{1}/Alexa.svc/json/Discovery/", hostText.Text, portText.Text), data);

            var items = JsonConvert.DeserializeObject<Alexa.SmartHome.V1.DiscoveryResponse>(dataToDeserialize);

            if (items.payload.discoveredAppliances == null)
            {
                MessageBox.Show(dataToDeserialize, "Error");
                return;
            }

            this.clearColumns();  
           
            foreach (var col in items.payload.discoveredAppliances)
            {
                var item = new ListViewItem(new[]
                    { col.applianceId,
                      col.manufacturerName,
                      col.modelName,
                      col.version,
                      col.friendlyName,
                      col.friendlyDescription,
                      col.isReachable, 
                      col.additionalApplianceDetails.dimmable,
                      col.additionalApplianceDetails.path
                });

                listView.Items.Add(item);
            }
            listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

            ModelCheck.Default.Host = hostText.Text;
            ModelCheck.Default.Port = portText.Text;
            ModelCheck.Default.AccessToken = accessToken.Text;
            this.statusText.Text = string.Format("Item Count:{0}", listView.Items.Count);
            ModelCheck.Default.Save();
        }

        private string jsonData(string url, string data)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = data;

                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            string retval = string.Empty;

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                retval = result.ToString();
            }
            return retval;
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            this.listView.ListViewItemSorter = new ListViewItemComparer(e.Column);
        }

        private void listView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listView.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    contextMenuStrip.Show(Cursor.Position);
                }
            }
        }

        private void contextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            commandStatus.Text = "OnOffRequest: ";

            try
            { 
                Alexa.SmartHome.V1.Appliance appliance = new Alexa.SmartHome.V1.Appliance();

                string idText = listView.SelectedItems[0].SubItems[0].Text;
                appliance.applianceId = Guid.Parse(idText).ToString("D");

                appliance.additionalApplianceDetails.dimmable = listView.SelectedItems[0].SubItems[7].Text;
                appliance.additionalApplianceDetails.path = listView.SelectedItems[0].SubItems[8].Text;

                string command = (e.ClickedItem.Name == turnItemOn.Name) ? "TURN_ON" :  "TURN_OFF";

                Alexa.SmartHome.V1.ControlSwitchOnOffRequest request = new Alexa.SmartHome.V1.ControlSwitchOnOffRequest(accessToken.Text, appliance, command);
                string data = JsonConvert.SerializeObject(request);

                var dataToDeserialize = jsonData(string.Format(@"https://{0}:{1}/Alexa.svc/json/Control/", hostText.Text, portText.Text), data);

                var response = JsonConvert.DeserializeObject<Alexa.SmartHome.V1.ControlResponse>(dataToDeserialize);

                commandStatus.Text = string.Format("OnOffRequest: Success = {0}", response.payload.success);
            }
            catch (Exception err)
            {
                commandStatus.Text = string.Format("OnOffRequest: Error = {0}", err.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ListViewToCSVUtils.ListViewToCSV(listView, @"c:\Tools\list.csv", true);
        }
    }

    class ListViewItemComparer : IComparer
    {
        private int col;
        public ListViewItemComparer()
        {
            col = 0;
        }
        public ListViewItemComparer(int column)
        {
            col = column;
        }
        public int Compare(object x, object y)
        {
            return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
        }
    }
    class ListViewToCSVUtils
    {
        public static void ListViewToCSV(ListView listView, string filePath, bool includeHidden)
        {
            //make header string
            StringBuilder result = new StringBuilder();
            WriteCSVRow(result, listView.Columns.Count, i => includeHidden || listView.Columns[i].Width > 0, i => listView.Columns[i].Text);

            //export data rows
            foreach (ListViewItem listItem in listView.Items)
                WriteCSVRow(result, listView.Columns.Count, i => includeHidden || listView.Columns[i].Width > 0, i => listItem.SubItems[i].Text);

            File.WriteAllText(filePath, result.ToString());
        }

        private static void WriteCSVRow(StringBuilder result, int itemsCount, Func<int, bool> isColumnNeeded, Func<int, string> columnValue)
        {
            bool isFirstTime = true;
            for (int i = 0; i < itemsCount; i++)
            {
                if (!isColumnNeeded(i))
                    continue;

                if (!isFirstTime)
                    result.Append(",");
                isFirstTime = false;

                result.Append(String.Format("\"{0}\"", columnValue(i)));
            }
            result.AppendLine();
        }
    }
}
