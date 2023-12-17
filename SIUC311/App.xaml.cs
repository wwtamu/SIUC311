using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Callisto.Controls;

namespace SIUC311
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                SIUC311.SuspensionManager.RegisterFrame(rootFrame, "SIUC311Frame");
                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                    try
                    {
                        await SIUC311.SuspensionManager.RestoreAsync();
                    }
                    catch (Exception) { }
                }

                SettingsPane.GetForCurrentView().CommandsRequested += App_CommandsRequested;
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), args.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        protected async override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs protocolArgs = args as ProtocolActivatedEventArgs;
                Frame rootFrame = Window.Current.Content as Frame; 
 
                // Do not repeat app initialization when the Window already has content, 
                // just ensure that the window is active 
 
                if (rootFrame == null) 
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();
                    SIUC311.SuspensionManager.RegisterFrame(rootFrame, "SIUC311Frame");
                    if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                        //TODO: Load state from previously suspended application
                        try
                        {
                            await SIUC311.SuspensionManager.RestoreAsync();
                        }
                        catch (Exception) { }
                    }

                    SettingsPane.GetForCurrentView().CommandsRequested += App_CommandsRequested;
                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                } 
 
                if (rootFrame.Content == null) 
                { 
                    if (!rootFrame.Navigate(typeof(MainPage))) 
                    { 
                        throw new Exception("Failed to create initial page"); 
                    } 
                } 
   
                // Ensure the current window is active 
                Window.Current.Activate(); 
            }
        }

        void App_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            SettingsCommand cmd =
                new SettingsCommand("queue-options", "Queue Options",
                (x) =>
                {
                    SettingsFlyout settings = new SettingsFlyout();
                    settings.HeaderBrush = new SolidColorBrush(Colors.Black);
                    settings.ContentBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 102, 0, 0));
                    settings.ContentForegroundBrush = new SolidColorBrush(Colors.White);
                    settings.FlyoutWidth = SettingsFlyout.SettingsFlyoutWidth.Narrow;
                    settings.HeaderText = "Settings";
                    settings.Content = new SettingsView();
                    settings.IsOpen = true;
                    SIUC311.SettingsView.SetAutoSubmitSetting(SIUC311.SettingsView.GetAutoSubmitSetting());
                });

            args.Request.ApplicationCommands.Add(cmd);

            SettingsCommand privacyStatement = 
                new SettingsCommand("privacy", "Privacy Statement", 
                async (x) =>
                {
                    await Launcher.LaunchUriAsync(new Uri("http://policies.siu.edu/policies/webprivacy.html"));
                });

            args.Request.ApplicationCommands.Add(privacyStatement);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            try
            {
                await SIUC311.SuspensionManager.SaveAsync();
            }
            catch (Exception) { }
            deferral.Complete();
        }
    }
}
