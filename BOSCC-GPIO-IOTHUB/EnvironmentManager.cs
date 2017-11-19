using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.Spi;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;

namespace BOSCC_GPIO_IOTHUB
{
    public class EnvironmentManager : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region Constants
        private readonly int GPIO_PIN_HOT_ID = 22; //in Robogaia source
        private readonly int GPIO_PIN_COLD_ID = 27;// in Robogaia source
        #endregion

        #region Privates
        private GpioPin heaterPort;
        private GpioPin acPort;
        private I2cDevice temperaturePort;
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty HeaterPowerOnProperty = DependencyProperty.Register("HeaterPowerOn", typeof(bool), typeof(EnvironmentManager), new PropertyMetadata(false));
        public static readonly DependencyProperty ACPowerOnProperty = DependencyProperty.Register("ACPowerOn", typeof(bool), typeof(EnvironmentManager), new PropertyMetadata(false));
        public static readonly DependencyProperty MeasuredTemperatureProperty = DependencyProperty.Register("MeasuredTemperature", typeof(int), typeof(EnvironmentManager), new PropertyMetadata(0));
        #endregion

        #region Public Properties
        public int MeasuredTemperature
        {
            get
            {
                int result = getTemperature();
                return result;
            }
        }



        public bool HeaterPowerOn
        {
            get
            {
                bool result = heaterPort.Read() == GpioPinValue.High;
                return result;
            }
            set
            {
                if (value)
                {
                    if (!HeaterPowerOn)
                    {
                        heaterPort.Write(GpioPinValue.High);
                        this.SetValue(HeaterPowerOnProperty, true);
                        OnPropertyChanged("HeaterPowerOn");

                    }
                }
                else
                {
                    if (HeaterPowerOn)
                    {
                        heaterPort.Write(GpioPinValue.Low);
                        this.SetValue(HeaterPowerOnProperty, false);
                        OnPropertyChanged("HeaterPowerOn");

                    }

                }
            }
        }

        public bool ACPowerOn
        {
            get
            {
                bool result = acPort.Read() == GpioPinValue.High;
                return result;
            }
            set
            {
                if (value)
                {
                    if (!ACPowerOn)
                    {
                        acPort.Write(GpioPinValue.High);
                        this.SetValue(ACPowerOnProperty, true);
                        OnPropertyChanged("ACPowerOn");
                    }
                }
                else
                {
                    if (ACPowerOn)
                    {
                        acPort.Write(GpioPinValue.Low);
                        this.SetValue(ACPowerOnProperty, false);
                        OnPropertyChanged("ACPowerOn");
                    }

                }
            }
        }
        #endregion



        public EnvironmentManager()
        {
            
        }

        public bool Initialize()
        {
            bool result = false;
            if (ApiInformation.IsTypePresent(typeof(GpioController).Name))
            {
                InitializeServoPorts();
                InitializeTempPort();
                result = true;
            }
            return result;
        }

        private async void InitializeTempPort()
        {

            string deviceQuery = I2cDevice.GetDeviceSelector("I2C1");
            DeviceInformationCollection deviceCollection = await DeviceInformation.FindAllAsync(deviceQuery);
            string deviceId = deviceCollection[0].Id;

            I2cConnectionSettings settings = new I2cConnectionSettings(0x4d);
            settings.BusSpeed = I2cBusSpeed.StandardMode;
            settings.SharingMode = I2cSharingMode.Shared;

            temperaturePort = await I2cDevice.FromIdAsync(deviceId, settings);

            getTemperature();
        }

        public void InitializeServoPorts()
        {

            GpioController controller = GpioController.GetDefault();
            if (null == controller)
                throw new System.NullReferenceException("GPIO Controller Unavailable");
            this.heaterPort = controller.OpenPin(GPIO_PIN_HOT_ID);
            heaterPort.Write(GpioPinValue.Low);
            heaterPort.SetDriveMode(GpioPinDriveMode.Output);

            this.acPort = controller.OpenPin(GPIO_PIN_COLD_ID);
            acPort.Write(GpioPinValue.Low);
            acPort.SetDriveMode(GpioPinDriveMode.Output);

            
        }

        public void RefreshTemp()
        {
            OnPropertyChanged("MeasuredTemperature");
        }


        private int getTemperature()
        {
            int result = 0;
            if (temperaturePort != null)
            {
                byte[] buffer = new byte[2];
                temperaturePort.Read(buffer);
                result = (((buffer[0] << 8) + buffer[1]) / 4) * (9 / 5) + 32;
            }
            return result;
        }

        private void OnPropertyChanged(string v)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(v));
            }
        }


    }


}
