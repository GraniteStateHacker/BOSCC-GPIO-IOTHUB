using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.SharePoint.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BOSCC_GPIO_IOTHUB
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DispatcherTimer timer = new DispatcherTimer();
        EnvironmentManager manager = new EnvironmentManager();
        //ClientContext sharepoint = new ClientContext("https://bluemetal-my.sharepoint.com/personal/jimw_bluemetal_com/IoT/");
        ClientContext sharepoint = new ClientContext("https://insightonline-my.sharepoint.com/personal/jim_wilcox_insight_com/IoT/");


        public MainPage()
        {
            this.InitializeComponent();
            if (manager.Initialize())
            {
                this.DataContext = manager;
                sharepoint.Credentials = new SharePointOnlineCredentials(Credentials.Username, Credentials.Password);
                timer.Tick += Timer_Tick;
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Start();
            }
            else
            {
                notice.Text = "Please run this on a Remote Device with ARM architecture. Device must also support GPIO api.";
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            manager.RefreshTemp();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Web web = sharepoint.Web;
                List log = web.Lists.GetByTitle("IoTLog");
                ListItemCreationInformation newLogEntryTemplate = new ListItemCreationInformation();
                ListItem newLogEntry = log.AddItem(newLogEntryTemplate);
                newLogEntry["Title"] = "Device #BOSCC";
                newLogEntry["MeasuredTemperature"] = manager.MeasuredTemperature;
                newLogEntry["HeaterPowerOn"] = manager.HeaterPowerOn;
                newLogEntry["ACPowerOn"] = manager.ACPowerOn;
                newLogEntry.Update();
                await sharepoint.ExecuteQueryAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private async void iotHubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var client = DeviceClient.CreateFromConnectionString(
                    $"HostName=BOSCC-IOTHub.azure-devices.net;DeviceId=GraniteStHacker;SharedAccessKey={Credentials.LuisAccessKeyFromAzurePortal}", 
                    TransportType.Mqtt))
                {
                    var twinProperties = new TwinCollection();
                    twinProperties["MeasuredTemperature"] = manager.MeasuredTemperature;
                    twinProperties["HeaterPowerOn"] = manager.HeaterPowerOn;
                    twinProperties["ACPowerOn"] = manager.ACPowerOn;
                    twinProperties["Device_BOSCC"] = DateTime.Now.ToString();
                    await client.UpdateReportedPropertiesAsync(twinProperties);
                    Console.WriteLine("Done");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
