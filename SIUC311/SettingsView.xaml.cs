using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SIUC311
{
    public sealed partial class SettingsView : UserControl
    {
        internal static Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        
        //private static Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        private static SettingsView _sv;

        public SettingsView()
        {
            this.InitializeComponent();
            _sv = this;
            if (SIUC311.MainPage.Current.AnyQueued())
            {
                if (SIUC311.MainPage.Current.IsInSession())
                {
                    SubmitQueueButton.IsEnabled = true;
                }
                else
                {
                    SubmitQueueButton.IsEnabled = false;
                }
                DeleteQueueButton.IsEnabled = true;
            }
            else
            {
                SubmitQueueButton.IsEnabled = false;
                DeleteQueueButton.IsEnabled = false;
            }
            SettingsFlyout.Background = new SolidColorBrush(Color.FromArgb(255, 102, 0, 0));
            AutoSubmitToggleSwitch.Background = new SolidColorBrush(Color.FromArgb(255, 102, 0, 0));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleAutoSubmit(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    localSettings.Values["AutoSubmit"] = true;
                }
                else
                {
                    localSettings.Values["AutoSubmit"] = false;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public static void SetAutoSubmitSetting(bool value)
        {
            _sv.AutoSubmitToggleSwitch.IsOn = value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool GetAutoSubmitSetting()
        {
            return Convert.ToBoolean(localSettings.Values["AutoSubmit"]);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SubmitQueueButton_Clicked(object sender, RoutedEventArgs e)
        {
            //await SIUC311.MainPage.Current.SubmitQueue();
            if (await SIUC311.MainPage.Current.SubmitQueue())
            {
                SIUC311.MainPage.Current.RefreshQueue();
                //SIUC311.MainPage.Current.NotifyUser("Queue submitted at " + string.Format("{0:M/d/yyyy h:mm:ss tt}", DateTime.Now), NotifyType.ReportMessage);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteQueueButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (await SIUC311.MainPage.Current.RemoveQueue(true))
            {
                SIUC311.MainPage.Current.RefreshQueue();
                //SIUC311.MainPage.Current.NotifyUser("Queue removed at " + string.Format("{0:M/d/yyyy h:mm:ss tt}", DateTime.Now), NotifyType.ReportMessage);
            }
        }
    }
}
