using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.Collections;

namespace ModelCheck
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

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
            hostText.Text = @"alexa.quigleys.us";
            portText.Text = "8733";
        }



        private void queryButton_Click(object sender, EventArgs e)
        {

            string data = @"{""header"": {""namespace"": ""Discovery"",""name"": ""DiscoverAppliancesRequest"",""payloadVersion"": ""1""},""payload"": { ""accessToken"": ""amzn1.account.AHHO677DA2UVHYY724MCRSBYZOVQ""}}";

            var deserialized = jsonData(string.Format(@"https://{0}:{1}/Alexa.svc/json/Discovery/", hostText.Text, portText.Text), data);

            var items = JsonConvert.DeserializeObject<DiscoveryResponse>(deserialized);

            listView.Clear();
            listView.Columns.Clear();
            listView.Columns.Add("applianceId", -2, HorizontalAlignment.Left);
            listView.Columns.Add("manufacturerName", -2, HorizontalAlignment.Left);
            listView.Columns.Add("modelName", -2, HorizontalAlignment.Left);
            listView.Columns.Add("version", -2, HorizontalAlignment.Left);
            listView.Columns.Add("friendlyName", -2, HorizontalAlignment.Left);
            listView.Columns.Add("friendlyDescription", -2, HorizontalAlignment.Left);
            listView.Columns.Add("isReachable", -2, HorizontalAlignment.Left);
            listView.Columns.Add("dimmable", -2, HorizontalAlignment.Left);
            listView.Columns.Add("path", -2, HorizontalAlignment.Left);

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

    public class AlexaHeader
    {
        public string @namespace { get; set; }
        public string name { get; set; }
        public string payloadVersion { get; set; }
    }

    public class AlexaDiscoveryPayload
    {
        public AlexaDevice[] discoveredAppliances { get; set; }
    }

    public class AlexaDeviceDetails
    {
        public string dimmable { get; set; }
        public string path { get; set; }
    }

    public class AlexaDevice
    {
        public string applianceId { get; set; }
        public string manufacturerName { get; set; }
        public string modelName { get; set; }
        public string version { get; set; }
        public string friendlyName { get; set; }
        public string friendlyDescription { get; set; }
        public string isReachable { get; set; }
        public AlexaDeviceDetails additionalApplianceDetails { get; set; }
    }

    public class DiscoveryResponse
    {
        public AlexaHeader header { get; set; }
        public AlexaDiscoveryPayload payload { get; set; }
    }

}
