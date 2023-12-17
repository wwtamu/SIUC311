/***************************************************
 * SIU 311 Maintenance Reporting App
 * 
 * MainPage.xaml.cs
 * 
 * This file contains the bulk of the code for
 * the user interface and various web service calls.
 * 
 * @version 1.1
 * @author William Welling <wwelling110381@siu.edu>
 * 
 ***************************************************/ 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO.Compression;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Media.Capture;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.Devices.Geolocation;
using Windows.Devices.Sensors;
using Windows.System.UserProfile;
using Windows.ApplicationModel.Activation;
using Bing.Maps;
using Windows.UI.Input;
using Windows.System.Threading;
using Windows.Storage.Search;
using Windows.Devices.Enumeration;
using System.Runtime.CompilerServices;
using Windows.Networking.Connectivity;
using Windows.System;
using System.Net.Http;
using System.Net;

namespace SIUC311
{
    /// <summary>
    /// class MainPage
    /// 
    /// </summary>
    public sealed partial class MainPage : LayoutAwarePage
    {
        #region Global
        ///////////////////////////////////////////////////////////////////////////////////////////////////
        // Global variables
        public static MainPage Current;        

        // WCF Service Library
        SIUC311ServiceRef.I311ServiceClient SIU311Service;

        // file paths for current image/video storage
        private Windows.Foundation.Collections.IPropertySet appSettings;

        private static List<int> reportListIndex = new List<int>();
        private static List<ReportObject> reportObjectList = new List<ReportObject>();

        private static List<int> queuedListIndex = new List<int>();
        private static List<ReportObject> queuedObjectList = new List<ReportObject>();
        
        private Geolocator _geolocatorForTracker = null;            // 
        private Geolocator _geolocatorForRequest = null;            //
        private Geolocator _geolocatorForMap = null;                //
        private CoreDispatcher _cd = null;                          //
        private CancellationTokenSource _cts = null;                //
        private Compass _compass = null;                            // 
        private Inclinometer _inclinometer = null;                  //
        private static String reportType = "default";               //
        private static String queryReportType = "default";          //
        private static int simpleQuery = 0;                         // 0 Query all reports, 1 Query my reports
        private static bool queryByType = false;                    //        
        private static bool refreshMap = true;                      //
        private static bool updatedListForMap = false;              //
        private static bool messageDisplayed = false;               //
        private static bool positioning = false;                    //
        private static bool havePhoto = false;                      //
        private static bool haveVideo = false;                      //
        private static byte[] _photoBytes;                          //
        private static bool isQueryAllNew = true;                   //
        private static bool isQueryAllByTypeNew = true;             //
        private static bool isQueryMyNew = true;                    //
        private static bool isQueryMyByTypeNew = true;              //
        private static bool isQueryPaged = false;                   //
        private static LocationIcon10m _locationIcon10m;            //
        private static LocationIcon100m _locationIcon100m;          //
        private static LocationIcon2000m _locationIcon2000m;        //
        private static int[] sortSelect = { 0, 0, 0, 0, 0, 0 };     //
        private static int sortSelectCount = 0;                     //
        private static String sessionId = null;                     //
        private static int selectedNumber = 0;                      //
        private static double selectedLatitude = 0.0;               //
        private static double selectedLongitude = 0.0;              //
        private static bool selectedIsQueued = false;               //
        private static bool loading = false;                        //
        private static int pageSize = 10;                           //
        private static bool inSession = false;                      //
        private static int queuedReportCount = 0;                   //
        private static bool haveInternetAccess = false;             //
        private static StorageFolder reportFolder = null;           //
        private static StorageFolder pictureFolder = null;          //
        private static bool isAdmin = false;                        //
        private static bool reportsChanged = false;                 //
        private static int page = 0;                                //
        private static int lastPage = 0;                            //
        private static bool buildingsAdded = false;                 //

        private SIUC311.Manipulations.InputProcessor _inputProcessor;
        #endregion

        #region Main
        /// <summary>
        /// MainPage constructor        
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            _inputProcessor = new SIUC311.Manipulations.InputProcessor(ReportPopupGrid, null);

            // This is a static public property that will allow downstream pages to get 
            // a handle to the MainPage instance in order to call methods that are in this class.
            Current = this;

            // Setup
            SetWindowsUser();
            SetDNSDomain();

            RefreshQueue();
            InitHeadings();
            InitRadioButtons();

            if (haveInternetAccess = CheckForInternet())
            {
                //NotifyUser("Internet.", NotifyType.ReportMessage);
            }
            else
            {
                //NotifyUser("No internet.", NotifyType.ReportMessage);
            }

            appSettings = ApplicationData.Current.LocalSettings.Values;
            _cd = Window.Current.CoreWindow.Dispatcher;

            _geolocatorForTracker = new Geolocator(); // May not need all three. Just synchronize access to one.
            _geolocatorForRequest = new Geolocator();
            _geolocatorForMap = new Geolocator();

            _compass = Compass.GetDefault(); // Get the default compass object

            _inclinometer = Inclinometer.GetDefault(); // Get the default inlinometer object

            _locationIcon10m = new LocationIcon10m();
            _locationIcon100m = new LocationIcon100m();
            _locationIcon2000m = new LocationIcon2000m();

            // Assign an event handler for the compass reading-changed event
            if (_compass != null)
            {
                // Establish the report interval for all scenarios
                uint minReportInterval = _compass.MinimumReportInterval;
                uint reportInterval = minReportInterval > 16 ? minReportInterval : 16;
                _compass.ReportInterval = reportInterval;
                _compass.ReadingChanged += new TypedEventHandler<Compass, CompassReadingChangedEventArgs>(CompassReadingChanged);
            }
            else
            {
                DirectionTextblock.Text = "No compass";
            }

            // Assign an event handler for the inclinometer reading-changed event
            if (_inclinometer != null)
            {
                // Establish the report interval for all scenarios
                uint minReportInterval = _inclinometer.MinimumReportInterval;
                uint reportInterval = minReportInterval > 16 ? minReportInterval : 16;
                _inclinometer.ReportInterval = reportInterval;

                // Establish the event handler
                _inclinometer.ReadingChanged += new TypedEventHandler<Inclinometer, InclinometerReadingChangedEventArgs>(InclinometerReadingChanged);
            }
            else
            {
                DirectionTextblock.Text = "No inclinometer";
            }

            Application.Current.Suspending += (sender, args) => OnSuspending();
            Application.Current.Resuming += (sender, o) => OnResuming();

            Window.Current.VisibilityChanged += Current_VisibilityChanged;

            TitleGrid.Background = new SolidColorBrush(Color.FromArgb(255, 102, 0, 0));
            Map.MapType = MapType.Aerial;
        }


        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            UpdateTime(string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now));
            StartTracking();
            if (haveInternetAccess = CheckForInternet())
            {
                SIU311Service = new SIUC311ServiceRef.I311ServiceClient();

                if (inSession = await BeginSession())
                {
                    ShowReports();
                    ConnectButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                ConnectButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Invoked immediately before the Page is unloaded and is no longer the current source of a parent Frame.
        /// </summary>
        /// <param name="e">
        /// Event data that can be examined by overriding code. The event data is representative
        /// of the navigation that will unload the current Page unless canceled. The
        /// navigation can potentially be canceled by setting e.Cancel to true.
        /// </param>
        protected async override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
            StopTracking();
            base.OnNavigatingFrom(e);
            if (haveInternetAccess)
            {
                await EndSession();
            }
        }

        /// <summary>
        /// Invoked when App is suspended.
        /// Stops the camera and ends the session.
        /// </summary>
        internal async void OnSuspending()
        {
            if (inSession)
            {
                inSession = await EndSession();
            }
        }

        /// <summary>
        /// Invoked when the App is resumed.
        /// Starts the camera and begins a session.
        /// </summary>
        internal async void OnResuming()
        {
            if (haveInternetAccess = CheckForInternet())
            {
                if (inSession = await BeginSession())
                {
                    ShowReports();

                    ConnectButton.Visibility = Visibility.Collapsed;
                    if (queuedReportCount > 0)
                    {
                        await SubmitQueue();
                    }
                }
            }
            else
            {
                ConnectButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Invoked when the Apps visibility changes.
        /// Toggles the camera.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Current_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            //if (!e.Visible) { }
            //else { }
        }

        /// <summary>
        /// Determins if there is an internet connection.
        /// </summary>
        /// <returns></returns>
        private bool CheckForInternet()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            bool internet = connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            return internet;
        }

        /// <summary>
        /// Returns true if there are any reports queued. Otherwise, returns false.
        /// </summary>
        /// <returns></returns>
        public bool AnyQueued()
        {
            if (queuedReportCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the global variable inSession. True when in a session, else false.
        /// </summary>
        /// <returns></returns>
        public bool IsInSession()
        {
            return inSession;
        }

        /// <summary>
        /// Returns whether a report popup is open or not.
        /// </summary>
        /// <returns></returns>
        public bool IsReportPopupOpen()
        {
            return ReportPopup.IsOpen;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DescriptionKeyDown(object sender, KeyRoutedEventArgs e)
        {
            //handling code
            if (e.Key == VirtualKey.Enter)
            {
                LocationTextbox.Focus(FocusState.Pointer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LocationKeyDown(object sender, KeyRoutedEventArgs e)
        {
            //handling code
            if (e.Key == VirtualKey.Enter)
            {
                SubmitReportPopupButton.Focus(FocusState.Pointer);
            }
        }

        /// <summary> 
        /// Invoked from OnNavigatedFrom
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        private void StartTracking()
        {
            //NotifyUser("Waiting for update...", NotifyType.StatusMessage);
            _geolocatorForTracker.PositionChanged += new TypedEventHandler<Geolocator, PositionChangedEventArgs>(OnPositionChanged);
        }

        /// <summary> 
        /// Invoked from OnNavigatedTo
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        private void StopTracking()
        {
            _geolocatorForTracker.PositionChanged -= new TypedEventHandler<Geolocator, PositionChangedEventArgs>(OnPositionChanged);
        }

        /// <summary>
        /// Begins a session with the web service.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> BeginSession()
        {
            try
            {
                //NotifyUser("Starting session. Please wait . . .", NotifyType.ReportMessage);
                loading = true;
                LoadingProgressRing.IsActive = true;
                sessionId = await SIU311Service.BeginSessionAsync(UserNameBlock.Text);
                if (sessionId != null)
                {
                    inSession = true;
                    //NotifyUser("Session started at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.ReportMessage);
                    try
                    {
                        if (isAdmin = await SIU311Service.CheckPermissionsAsync(sessionId))
                        {
                            PermissionsBlock.Text = "Admin";
                            PopupPermissionsBlock.Text = "Admin";
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception)
            {
                PromptMessage("Unable to start session. Service endpoint cannot be established.\n\nReports will be queued for future submission when endpoint is available. \n\nIf you have internet access and are off campus please connect to AD.SIU.EDU domain via Network Connect VPN. Thank You.");
                NotifyUser("Reports Queueing", NotifyType.ReportMessage);
            }
            loading = false;
            LoadingProgressRing.IsActive = false;
            return inSession;
        }

        /// <summary>
        /// Ends a session with the web service.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> EndSession()
        {
            try
            {
                inSession = await SIU311Service.EndSessionAsync(sessionId);
                inSession = false;
                isQueryAllNew = true;
                isQueryAllByTypeNew = true;
                isQueryMyNew = true;
                isQueryMyByTypeNew = true;
            }
            catch (Exception)
            {
                inSession = false;
                PromptMessage("Unable to end session. Service endpoint cannot be established.\n\n");
            }
            return inSession;
        }

        /// <summary>
        /// Resets a session with the web service.
        /// </summary>
        private async void ResetSession()
        {
            isQueryAllNew = true;
            isQueryAllByTypeNew = true;
            isQueryMyNew = true;
            isQueryMyByTypeNew = true;
            isQueryPaged = true;
            bool lks = inSession;
            try
            {
                if (inSession = await SIU311Service.IsSessionAliveAsync(sessionId))
                {
                    ShowReports();
                }
                else
                {
                    inSession = false;
                }
            }
            catch (Exception)
            {
                if (lks)
                {
                    PromptMessage("Unable to reset session. Session lost. Service endpoint cannot be established.\n\n");
                }
                inSession = false;
            }
        }

        /// <summary>
        /// Updates textblocks for latitude, longitude, and 
        /// accuracy to display on the screen
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="acc"></param>
        public void UpdateGeolocation(String lat, String lon, String acc)
        {
            LatitudeTextblock.Text = lat;
            LongitudeTextblock.Text = lon;
            AccuracyTextblock.Text = acc;
        }

        /// <summary>
        /// Updates textblocks for time to display on the screen
        /// </summary>
        /// <param name="time"></param>
        public void UpdateTime(String time)
        {
            TimeTextblock.Text = time;
        }

        /// <summary>
        /// Switch for displaying messages to different locations
        /// on screen from one function and a message type
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    NotificationTextblock.Foreground = new SolidColorBrush(Colors.Blue);
                    NotificationTextblock.Text = strMessage;
                    break;
                case NotifyType.DisplayMessage:
                    NotificationTextblock.Foreground = new SolidColorBrush(Colors.Black);
                    NotificationTextblock.Text = strMessage;
                    break;
                case NotifyType.ReportMessage:
                    NotificationTextblock.Foreground = new SolidColorBrush(Colors.Green);
                    NotificationTextblock.Text = strMessage;
                    break;
                case NotifyType.QueueMessage:
                    NotificationTextblock.Foreground = new SolidColorBrush(Colors.Orange);
                    NotificationTextblock.Text = strMessage;
                    break;
                case NotifyType.ErrorMessage:
                    NotificationTextblock.Foreground = new SolidColorBrush(Colors.Red);
                    NotificationTextblock.Text = strMessage;
                    break;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 
        /// </summary>
        private async void ShowVideo()
        {
            if (haveVideo == true)
            {
                CapturedVideoHolder.Visibility = Visibility.Collapsed;
                if (appSettings.ContainsKey(Constants.videoKey))
                {
                    object filePath;
                    if (appSettings.TryGetValue(Constants.videoKey, out filePath) && filePath.ToString() != "")
                    {
                        await ReloadVideo(filePath.ToString());
                    }
                }
            }
            else
            {
                CapturedVideo.Visibility = Visibility.Collapsed;
                CapturedVideoHolder.Visibility = Visibility.Visible;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        private void UpdateSortOrderTextBox(int i)
        {
            switch (i)
            {
                case 0: { ByTypeSelectedTextblock.Text = Convert.ToString(Convert.ToInt32(ByTypeSelectedTextblock.Text) - 1); } break;
                case 1: { ByDateSelectedTextblock.Text = Convert.ToString(Convert.ToInt32(ByDateSelectedTextblock.Text) - 1); } break;
                case 2: { ByAuthorSelectedTextblock.Text = Convert.ToString(Convert.ToInt32(ByAuthorSelectedTextblock.Text) - 1); } break;
                case 3: { ByStatusSelectedTextblock.Text = Convert.ToString(Convert.ToInt32(ByStatusSelectedTextblock.Text) - 1); } break;
                case 4: { ByPrioritySelectedTextblock.Text = Convert.ToString(Convert.ToInt32(ByPrioritySelectedTextblock.Text) - 1); } break;
                case 5: { ByFrequencySelectedTextblock.Text = Convert.ToString(Convert.ToInt32(ByFrequencySelectedTextblock.Text) - 1); } break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void ResetQuery()
        {
            isQueryAllNew = true;
            isQueryAllByTypeNew = true;
            isQueryMyNew = true;
            isQueryMyByTypeNew = true;
        }

        /// <summary>
        /// button handler to capture photo
        /// Invokes camera control to take photo and crop
        /// converts image to byte array to store in database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            //NotifyUser("Attempting to capture photo", NotifyType.StatusMessage);
            try
            {
                // Using Windows.Media.Capture.CameraCaptureUI API to capture a photo
                CameraCaptureUI dialog = new CameraCaptureUI();
                Size aspectRatio = new Size(16, 9);
                dialog.PhotoSettings.AllowCropping = true;                
                dialog.PhotoSettings.CroppedAspectRatio = aspectRatio; 
                dialog.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Png;
                dialog.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.MediumXga; //.HighestAvailable;

                StorageFile file = await dialog.CaptureFileAsync(CameraCaptureUIMode.Photo);

                _photoBytes = null;
                _photoBytes = await ConvertImagetoByte(file);

                if (file != null)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        bitmapImage.SetSource(fileStream);
                    }
                    CapturedPhoto.Source = bitmapImage;
                    ReportFormPhoto.Source = bitmapImage;

                    // Store the file path in Application Data
                    appSettings[Constants.photoKey] = file.Path;
                }
                else
                {
                    NotifyUser("No photo.", NotifyType.StatusMessage);
                }
            }
            catch (Exception)
            {
                //NotifyUser("ERROR TAKING PHOTO", NotifyType.ErrorMessage);
            }
            GetGeolocation();
            havePhoto = true;
            // open the Popup if it not open already
            if (!ReportFormPopup.IsOpen) { ReportFormPopup.IsOpen = true; }
        }

        /// <summary>
        /// button handler to capture video
        /// Invokes camera controls to take video
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CaptureVideo_Click(object sender, RoutedEventArgs e)
        {
            //NotifyUser("Attempting to take video", NotifyType.StatusMessage);            
            try
            {
                // Using Windows.Media.Capture.CameraCaptureUI API to capture a photo
                CameraCaptureUI dialog = new CameraCaptureUI();
                dialog.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;

                StorageFile file = await dialog.CaptureFileAsync(CameraCaptureUIMode.Video);
                if (file != null)
                {
                    IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
                    CapturedVideo.SetSource(fileStream, "video/mp4");

                    // Store the file path in Application Data
                    appSettings[Constants.videoKey] = file.Path;
                }
                else
                {
                    NotifyUser("No video captured.", NotifyType.StatusMessage);
                }
            }
            catch (Exception)
            {
                NotifyUser("ERROR RECORDING VIDEO", NotifyType.ErrorMessage);
            }
            GetGeolocation();
            haveVideo = true;
            ShowVideo();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MakeReport_Click(object sender, RoutedEventArgs e)
        {
            // close the Popup if it open already
            if (ReportFormPopup.IsOpen) { ReportFormPopup.IsOpen = false; }

            ReportFormPopup.IsOpen = true;
        }
        
        /// <summary>
        /// - button handler to make a report
        /// - checks to see if photo is captured
        /// - send insert request to WCF Service Library
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SubmitReport_Clicked(object sender, RoutedEventArgs e)
        {   
            if (havePhoto == false)
            {
                await WaitablePromptMessage("You must take a photo to make a report.\n\nPress Take Photo and take a picture of the site needing attention.\n\nThank You.");
                //ButtonAutomationPeer peer = new ButtonAutomationPeer(CapturePhoto);
                //peer.Invoke();
            }
            else
            {
                ReportFormProgressBar.Visibility = Visibility.Visible;
                try
                {
                    //NotifyUser("Processing report . . . ", NotifyType.ReportMessage);
                    byte[] photo = _photoBytes;

                    if (haveInternetAccess)
                    {
                        try
                        {
                            inSession = await SIU311Service.IsSessionAliveAsync(sessionId); // SLOW TAKES TIME 
                        }
                        catch (Exception)
                        {
                            inSession = false;
                        }

                        if (inSession)
                        {
                            //NotifyUser("Session checked at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.ReportMessage);
                            if (await SIU311Service.InsertReportAsync(sessionId,
                                new SIUC311ServiceRef.ReportObject
                                {
                                    ReportType = reportType,
                                    ReportAuthor = UserNameBlock.Text,
                                    ReportDescription = DescriptionTextbox.Text.Trim(),
                                    ReportLocation = LocationTextbox.Text.Trim(),
                                    ReportTime = DateTime.Now,
                                    ReportLatitude = LatitudeTextblock.Text.Trim(),
                                    ReportLongitude = LongitudeTextblock.Text.Trim(),
                                    ReportAccuracy = AccuracyTextblock.Text.Trim(),
                                    ReportDirection = DirectionTextblock.Text.Trim()
                                }))
                            {
                                //NotifyUser("Report Accepted at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.QueueMessage);

                                //NotifyUser("Attempting to submit photo", NotifyType.StatusMessage);
                                if (await SIU311Service.InsertPhotoAsync(sessionId,
                                    new SIUC311ServiceRef.PhotoObject
                                    {
                                        ReportPhoto = photo
                                    }))
                                {
                                    ResetReportForm();
                                }
                                else
                                {
                                    NotifyUser("Unable to upload photo", NotifyType.ReportMessage);
                                }
                            }
                            else
                            {
                                NotifyUser("Report Rejected. Try Again.", NotifyType.ReportMessage);
                            }
                        }
                        else
                        {
                            NotifyUser("Cannot connect to endpoint.", NotifyType.ErrorMessage);
                            EnqueueReport();
                            RefreshQueue();
                        }
                    }
                    else
                    {
                        EnqueueReport();
                        RefreshQueue();
                    }
                    ResetSession();
                }
                catch (Exception ex)
                {
                    PromptMessage(ex.StackTrace);
                }
                ReportFormProgressBar.Visibility = Visibility.Collapsed;
                // close the Popup if it open already
                if (ReportFormPopup.IsOpen) { ReportFormPopup.IsOpen = false; }
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        public async void RefreshQueue()
        {
            try
            {
                reportFolder = await KnownFolders.DocumentsLibrary.CreateFolderAsync("311_Reports_Folder", CreationCollisionOption.OpenIfExists);
            }
            catch (Exception) { /*NotifyUser("DOCUMENT ERROR", NotifyType.ReportMessage);*/ }

            try
            {
                pictureFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync("311_Pictures_Folder", CreationCollisionOption.OpenIfExists);
            }
            catch (Exception) { /*NotifyUser("PICTURE ERROR", NotifyType.ReportMessage);*/ }

            var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { ".txt" });
            var query = reportFolder.CreateFileQueryWithOptions(queryOptions);
            var report_files = await query.GetFilesAsync();

            queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { ".png" });
            query = pictureFolder.CreateFileQueryWithOptions(queryOptions);
            var picture_files = await query.GetFilesAsync();

            //if (report_files.Count != picture_files.Count)
            //{
            //   await RemoveQueue(true);
            //}

            if ((queuedReportCount = report_files.Count) > 0)
            {
                ShowQueue(report_files, picture_files);
            }
            else
            {
                QueuedListView.Items.Clear();
                queuedListIndex.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rf"></param>
        /// <param name="pf"></param>
        private async void ShowQueue(IReadOnlyList<StorageFile> rf, IReadOnlyList<StorageFile> pf)
        {
            ObservableCollection<ReportObject> QueuedReportsList = new ObservableCollection<ReportObject>();
            QueuedListView.Items.Clear();
            queuedListIndex.Clear();
            int queued_count = 0;
            foreach (StorageFile report_file in rf)
            {
                queued_count++;
                string reportString = await FileIO.ReadTextAsync(report_file);
                string[] report_split = reportString.Split(new char[] { '|' }, StringSplitOptions.None);

                ReportObject qro = new ReportObject(-queued_count,
                                                    report_split[0], report_split[1],
                                                    report_split[2], report_split[3],
                                                    ConvertToDateTime(report_split[4]), report_split[5],
                                                    report_split[6], report_split[7], report_split[8]);
                QueuedReportsList.Add(qro);
            }
            PopulateQueue(QueuedReportsList, pf);
        }

        /// <summary>
        /// 
        /// </summary>
        private async void EnqueueReport()
        {
            bool ps = false;
            bool rs = false;
            if (reportFolder != null)
            {
                StorageFile reportFile = null;
                try
                {
                    queuedReportCount++;
                    reportFile = await reportFolder.CreateFileAsync(queuedReportCount + "_" + reportType + ".txt", CreationCollisionOption.ReplaceExisting);                    
                }
                catch (Exception) { /*NotifyUser("FILE ERROR", NotifyType.ReportMessage);*/ }

                if (reportFile != null)
                {
                    if (DescriptionTextbox.Text.Length < 1) { DescriptionTextbox.Text = "NA"; }
                    if (LocationTextbox.Text.Length < 1) { LocationTextbox.Text = "NA"; }
                    if (LatitudeTextblock.Text.Length < 1) { LatitudeTextblock.Text = "NA"; }
                    if (LongitudeTextblock.Text.Length < 1) { LongitudeTextblock.Text = "NA"; }
                    if (AccuracyTextblock.Text.Length < 1) { AccuracyTextblock.Text = "NA"; }
                    if (DirectionTextblock.Text.Length < 1) { DirectionTextblock.Text = "NA"; }

                    try
                    {
                        await Windows.Storage.FileIO.WriteTextAsync(reportFile, reportType + "|" +
                                                                                UserNameBlock.Text + "|" +
                                                                                DescriptionTextbox.Text.Trim() + "|" +
                                                                                LocationTextbox.Text.Trim() + "|" +
                                                                                DateTime.Now + "|" +
                                                                                LatitudeTextblock.Text.Trim() + "|" +
                                                                                LongitudeTextblock.Text.Trim() + "|" +
                                                                                AccuracyTextblock.Text.Trim() + "|" +
                                                                                DirectionTextblock.Text.Trim());
                        rs = true;
                        //NotifyUser("Report stored", NotifyType.ReportMessage);

                    }
                    catch (Exception) { /*NotifyUser("ERROR", NotifyType.ReportMessage);*/ }
                }
            }
            if (pictureFolder != null)
            {
                StorageFile pictureFile = null;
                try
                {
                    pictureFile = await pictureFolder.CreateFileAsync(queuedReportCount + "_" + reportType + ".png", CreationCollisionOption.ReplaceExisting);
                }
                catch (Exception) { /*NotifyUser("FILE ERROR", NotifyType.ReportMessage);*/ }

                if (pictureFile != null)
                {
                    try
                    {
                        await Windows.Storage.FileIO.WriteBytesAsync(pictureFile, _photoBytes);
                        //NotifyUser("Photo stored", NotifyType.ReportMessage);
                        ps = true;
                    }
                    catch (Exception) { /*NotifyUser("ERROR", NotifyType.ReportMessage);*/ }
                }
            }
            if (rs && ps)
            {
                //NotifyUser("Report Queued at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.ReportMessage);
            }
            ResetReportForm();
        }

        /// <summary>
        /// 
        /// </summary>
        private void LockControls()
        {
            Next.IsEnabled = false;
            Previous.IsEnabled = false;
            AllReportsRadioButton.IsEnabled = false;
            MyReportsRadioButton.IsEnabled = false;
            ReportsByTypeCheckBox.IsEnabled = false;
            ReportTypeCombobox.IsEnabled = false;
            ReportsSortedByTypeCheckBox.IsEnabled = false;
            SortByTypeCheckBox.IsEnabled = false;
            SortByDateCheckBox.IsEnabled = false;
            SortByAuthorCheckBox.IsEnabled = false;
            SortByStatusCheckBox.IsEnabled = false;
            SortByPriorityCheckBox.IsEnabled = false;
            SortByFrequencyCheckBox.IsEnabled = false;
        }

        /// <summary>
        /// 
        /// </summary>
        private void UnLockControls()
        {
            Next.IsEnabled = true;
            Previous.IsEnabled = true;
            AllReportsRadioButton.IsEnabled = true;
            MyReportsRadioButton.IsEnabled = true;
            ReportsByTypeCheckBox.IsEnabled = true;
            ReportTypeCombobox.IsEnabled = true;
            ReportsSortedByTypeCheckBox.IsEnabled = true;
            SortByTypeCheckBox.IsEnabled = true;
            SortByDateCheckBox.IsEnabled = true;
            if (MyReportsRadioButton.IsChecked != true)
            {
                SortByAuthorCheckBox.IsEnabled = true;
            }
            SortByStatusCheckBox.IsEnabled = true;
            SortByPriorityCheckBox.IsEnabled = true;
            SortByFrequencyCheckBox.IsEnabled = true;
        }

        /// <summary>        
        /// - 10 reports per page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ShowReports()
        {   
            LockControls();
            if (loading == false)
            {
                try
                {
                    inSession = await SIU311Service.IsSessionAliveAsync(sessionId);
                }
                catch (Exception)
                {
                    inSession = false;
                }
                if (inSession)
                {                    
                    ObservableCollection<SIUC311ServiceRef.ReportObject> ReportsList = new ObservableCollection<SIUC311ServiceRef.ReportObject>();
                    ObservableCollection<int> paging_state = new ObservableCollection<int>();
                    LoadingProgressRing.IsActive = true;
                    loading = true;
                    try
                    {
                        reportListIndex.Clear();
                        switch (simpleQuery)
                        {
                            case 0: // Query all reports
                                {
                                    if (queryByType)
                                    {
                                        ReportsList = await SIU311Service.GetAllReportsByTypeAsync(queryReportType, sessionId, isQueryAllByTypeNew, 0, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                        paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                        HideOrShowNextorPrevious(paging_state);
                                        PopulateList(ReportsList, false);
                                        if (isQueryAllByTypeNew)
                                        {
                                            isQueryAllByTypeNew = false;
                                        }
                                    }
                                    else
                                    {
                                        ReportsList = await SIU311Service.GetAllReportsAsync(sessionId, isQueryAllNew, 0, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                        paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                        HideOrShowNextorPrevious(paging_state);
                                        PopulateList(ReportsList, false);
                                        if (isQueryAllNew)
                                        {
                                            isQueryAllNew = false;
                                        }
                                    }
                                } break;
                            case 1: // Query my reports
                                {
                                    if (queryByType)
                                    {
                                        ReportsList = await SIU311Service.GetMyReportsByTypeAsync(queryReportType, sessionId, isQueryMyByTypeNew, 0, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                        paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                        HideOrShowNextorPrevious(paging_state);
                                        PopulateList(ReportsList, true);
                                        if (isQueryMyByTypeNew)
                                        {
                                            isQueryMyByTypeNew = false;
                                        }
                                    }
                                    else
                                    {
                                        ReportsList = await SIU311Service.GetMyReportsAsync(sessionId, isQueryMyNew, 0, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                        paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                        HideOrShowNextorPrevious(paging_state);
                                        PopulateList(ReportsList, true);
                                        if (isQueryMyNew)
                                        {
                                            isQueryMyNew = false;
                                        }
                                    }
                                } break;
                            default: break;
                        }
                        ReportsListView.Items.Clear();
                        LoadingProgressRing.IsActive = false;
                        page = paging_state[0];
                        if (paging_state[1] % 10 > 0)
                        {
                            lastPage = (paging_state[1] / 10);
                        }
                        else
                        {
                            lastPage = (paging_state[1] / 10) - 1;
                        }
                        loading = false;
                        //NotifyUser("Reports displayed at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.DisplayMessage);
                    }
                    catch (Exception sre)
                    {
                        PromptMessage("ERROR SHOWING REPORTS\n" + sre.StackTrace);
                    }
                }
            }
        }

        /// <summary>
        /// The next and previous buttons will not display unless needed
        /// 
        /// </summary>
        /// <param name="rl"></param>
        private void HideOrShowNextorPrevious(ObservableCollection<int> paging_state)
        {
            int page = paging_state[0];
            int total = paging_state[1];

            if (page < (total / pageSize))
            {
                Next.Visibility = Visibility.Visible;
                if (total - ((page + 1) * pageSize) == 0)
                {
                    Next.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                Next.Visibility = Visibility.Collapsed;
            }

            if (page > 0)
            {
                Previous.Visibility = Visibility.Visible;
            }
            else
            {
                Previous.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// - performs service request to get next page within a specific query
        /// - builds list from next 10 reports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            LockControls();
            if (loading == false)
            {
                try
                {
                    inSession = await SIU311Service.IsSessionAliveAsync(sessionId); // SLOW TAKES TIME 
                }
                catch (Exception)
                {
                    inSession = false;
                }
                if (inSession)
                {   
                    ObservableCollection<SIUC311ServiceRef.ReportObject> ReportsList = new ObservableCollection<SIUC311ServiceRef.ReportObject>();
                    ObservableCollection<int> paging_state = new ObservableCollection<int>();
                    LoadingProgressRing.IsActive = true;
                    loading = true;
                    reportListIndex.Clear();
                    switch (simpleQuery)
                    {
                        case 0: // Query all reports
                            {
                                if (queryByType)
                                {
                                    ReportsList = await SIU311Service.GetAllReportsByTypeAsync(queryReportType, sessionId, isQueryAllByTypeNew, 1, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                    paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                    HideOrShowNextorPrevious(paging_state);
                                    PopulateList(ReportsList, false);
                                }
                                else
                                {
                                    ReportsList = await SIU311Service.GetAllReportsAsync(sessionId, isQueryAllNew, 1, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                    paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                    HideOrShowNextorPrevious(paging_state);
                                    PopulateList(ReportsList, false);
                                }
                            } break;
                        case 1: // Query my reports
                            {
                                if (queryByType)
                                {
                                    ReportsList = await SIU311Service.GetMyReportsByTypeAsync(queryReportType, sessionId, isQueryMyByTypeNew, 1, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                    paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                    HideOrShowNextorPrevious(paging_state);
                                    PopulateList(ReportsList, true);
                                }
                                else
                                {
                                    ReportsList = await SIU311Service.GetMyReportsAsync(sessionId, isQueryMyNew, 1, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                    paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                    HideOrShowNextorPrevious(paging_state);
                                    PopulateList(ReportsList, true);
                                }
                            } break;
                        default: break;
                    }
                    ReportsListView.Items.Clear();
                    LoadingProgressRing.IsActive = false;
                    page = paging_state[0];
                    lastPage = paging_state[1];
                    loading = false;
                    //NotifyUser("Reports displayed at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.DisplayMessage);
                }
            }
        }

        /// <summary>
        /// - performs service request to get previous page within a specific query
        /// - builds list from previous 10 reports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            LockControls();
            if (loading == false)
            {
                try
                {
                    inSession = await SIU311Service.IsSessionAliveAsync(sessionId); // SLOW TAKES TIME 
                }
                catch (Exception)
                {
                    inSession = false;
                }
                if (inSession)
                {
                    ObservableCollection<SIUC311ServiceRef.ReportObject> ReportsList = new ObservableCollection<SIUC311ServiceRef.ReportObject>();
                    ObservableCollection<int> paging_state = new ObservableCollection<int>();
                    LoadingProgressRing.IsActive = true;
                    loading = true;
                    reportListIndex.Clear();
                    switch (simpleQuery)
                    {
                        case 0: // Query all reports
                            {
                                if (queryByType)
                                {
                                    ReportsList = await SIU311Service.GetAllReportsByTypeAsync(queryReportType, sessionId, isQueryAllByTypeNew, -1, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                    paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                    HideOrShowNextorPrevious(paging_state);
                                    PopulateList(ReportsList, false);
                                }
                                else
                                {
                                    ReportsList = await SIU311Service.GetAllReportsAsync(sessionId, isQueryAllNew, -1, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                    paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                    HideOrShowNextorPrevious(paging_state);
                                    PopulateList(ReportsList, false);
                                }
                                ReportsListView.Items.Clear();
                            } break;
                        case 1: // Query my reports
                            {
                                if (queryByType)
                                {
                                    ReportsList = await SIU311Service.GetMyReportsByTypeAsync(queryReportType, sessionId, isQueryMyByTypeNew, -1, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                    paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                    HideOrShowNextorPrevious(paging_state);
                                    PopulateList(ReportsList, true);
                                }
                                else
                                {
                                    ReportsList = await SIU311Service.GetMyReportsAsync(sessionId, isQueryMyNew, -1, isQueryPaged, new ObservableCollection<int>(sortSelect));
                                    paging_state = await SIU311Service.GetPagingStateAsync(sessionId);
                                    HideOrShowNextorPrevious(paging_state);
                                    PopulateList(ReportsList, true);
                                }
                                ReportsListView.Items.Clear();
                            } break;
                        default: break;
                    }
                    LoadingProgressRing.IsActive = false;
                    page = paging_state[0];
                    lastPage = paging_state[1];
                    loading = false;
                    //NotifyUser("Reports displayed at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.DisplayMessage);
                }
            }
        }

        /// <summary>
        /// Populates list from response of web service
        /// </summary>
        /// <param name="ReportsList"></param>
        private async void PopulateList(ObservableCollection<SIUC311ServiceRef.ReportObject> ReportsList, bool byOwner)
        {
            // add condition from checkbox
            if (refreshMap)
            {
                ClearMap();
                reportObjectList.Clear();
            }            

            foreach (var Rep in ReportsList)
            {
                Image image = new Image();

                var photoObject = await SIU311Service.GetPhotoAsync(Rep.ReportId);

                BitmapImage bimage;

                if (photoObject != null)
                {
                    bimage = await ByteToImage(photoObject.ReportPhoto);
                }
                else
                {
                    bimage = new BitmapImage(new Uri("ms-appx:///Assets/ImagePlaceHolder.png"));
                }

                string status = await SIU311Service.GetStatusAsync(Rep.ReportId);

                image.Source = bimage;

                GridViewItem ReportView = new GridViewItem();
                GridViewItem ImageView = new GridViewItem();

                StackPanel listSP = new StackPanel();

                LinearGradientBrush lgb = new LinearGradientBrush();

                lgb.StartPoint = new Point(.5, 0);
                lgb.EndPoint = new Point(.5, 1);

                GradientStop lggs = new GradientStop();
                lggs.Color = Color.FromArgb(255, 217, 214, 203);
                lggs.Offset = 0.0;
                lgb.GradientStops.Add(lggs);

                GradientStop ggs = new GradientStop();
                ggs.Color = Color.FromArgb(255, 108, 108, 108);
                ggs.Offset = 1.25;
                lgb.GradientStops.Add(ggs);

                listSP.Background = lgb;

                listSP.Orientation = Orientation.Horizontal;

                StackPanel reportSP = new StackPanel();

                reportListIndex.Add(Rep.ReportId);

                StackPanel idStatusSP = new StackPanel();
                idStatusSP.Orientation = Orientation.Horizontal;

                TextBlock ridTB = new TextBlock() { Text = Rep.ReportId.ToString() };
                ridTB.Foreground = new SolidColorBrush(Colors.Black);

                TextBlock statusTB = new TextBlock() { Text = " : " + status };
                statusTB.Foreground = new SolidColorBrush(Colors.Black);

                idStatusSP.Children.Add(ridTB);
                idStatusSP.Children.Add(statusTB);

                reportSP.Children.Add(idStatusSP);

                TextBlock rtypeTB = new TextBlock() { Text = Rep.ReportType };
                rtypeTB.Foreground = new SolidColorBrush(Colors.Black);
                reportSP.Children.Add(rtypeTB);

                if (!byOwner)
                {
                    TextBlock rownTB = new TextBlock() { Text = Rep.ReportAuthor };
                    rownTB.Foreground = new SolidColorBrush(Colors.Black);
                    reportSP.Children.Add(rownTB);
                }

                TextBlock rtimeTB = new TextBlock() { Text = Rep.ReportTime.ToString() };
                rtimeTB.Foreground = new SolidColorBrush(Colors.Black);
                reportSP.Children.Add(rtimeTB);

                ReportObject reportObject = new ReportObject(Rep.ReportId,
                                                             Rep.ReportType,
                                                             Rep.ReportAuthor,
                                                             Rep.ReportDescription,
                                                             Rep.ReportLocation,
                                                             Rep.ReportTime,
                                                             Rep.ReportLatitude,
                                                             Rep.ReportLongitude,
                                                             Rep.ReportAccuracy,
                                                             Rep.ReportDirection);

                reportObjectList.Add(reportObject);

                if (!(reportObject.ReportLatitude == "NA" || reportObject.ReportLatitude == "NA"))
                {
                    try
                    {
                        AddToMap(reportObject);
                    }
                    catch (Exception) { }
                }

                image.Height = 110;
                image.Width = 175;
                ImageView.Content = image;

                ReportView.Content = reportSP;

                ImageView.AddHandler(UIElement.TappedEvent, new TappedEventHandler(ImageSelected), true);
                ReportView.AddHandler(UIElement.TappedEvent, new TappedEventHandler(ReportSelected), true);

                listSP.Children.Add(ReportView);
                listSP.Children.Add(ImageView);

                ReportsListView.Items.Add(listSP);
            }
            UnLockControls();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageSelected(object sender, TappedRoutedEventArgs e)
        {
            if (ReportsListView.Items.Count > 0)
            {
                if (ReportsListView.SelectedIndex != reportListIndex.IndexOf(Convert.ToInt32(((TextBlock)((StackPanel)((StackPanel)((GridViewItem)((StackPanel)((GridViewItem)sender).Parent).Children[0]).Content).Children[0]).Children[0]).Text)))
                {
                    ReportsListView.SelectedIndex = reportListIndex.IndexOf(Convert.ToInt32(((TextBlock)((StackPanel)((StackPanel)((GridViewItem)((StackPanel)((GridViewItem)sender).Parent).Children[0]).Content).Children[0]).Children[0]).Text));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportSelected(object sender, TappedRoutedEventArgs e)
        {
            if (ReportsListView.Items.Count > 0)
            {
                if (ReportsListView.SelectedIndex != reportListIndex.IndexOf(Convert.ToInt32(((TextBlock)((StackPanel)((StackPanel)((GridViewItem)sender).Content).Children[0]).Children[0]).Text)))
                {
                    ReportsListView.SelectedIndex = reportListIndex.IndexOf(Convert.ToInt32(((TextBlock)((StackPanel)((StackPanel)((GridViewItem)sender).Content).Children[0]).Children[0]).Text));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ReportsListView_Clicked(object sender, TappedRoutedEventArgs e)
        {
            if (ReportsListView.Items.Count > 0)
            {
                StackPanel rsp = (StackPanel)((GridViewItem)((StackPanel)((ListView)sender).SelectedValue).Children[0]).Content;
                await ShowReportPopup(((TextBlock)((StackPanel)rsp.Children[0]).Children[0]).Text, false);
            }
        }

        /// <summary>
        /// Populates list from response of web service
        /// </summary>
        /// <param name="ReportsList"></param>
        private async void PopulateQueue(ObservableCollection<ReportObject> QueuedList, IReadOnlyList<StorageFile> pf)
        {
            //if (refreshMap)
            //{
            //    ClearMap();
            //}

            queuedObjectList.Clear();

            int count = 0;
            foreach (var Que in QueuedList)
            {
                Image image = new Image();
                count++;
                var file = await pictureFolder.GetFileAsync(count + "_" + Que.ReportType.ToString() + ".png");

                var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                var img = new BitmapImage();
                img.SetSource(fileStream);

                image.Source = img;

                GridViewItem ReportView = new GridViewItem();
                GridViewItem ImageView = new GridViewItem();

                StackPanel listSP = new StackPanel();

                LinearGradientBrush lgb = new LinearGradientBrush();

                lgb.StartPoint = new Point(.5, 0);
                lgb.EndPoint = new Point(.5, 1);

                GradientStop lggs = new GradientStop();
                lggs.Color = Color.FromArgb(255, 217, 214, 203);
                lggs.Offset = 0.0;
                lgb.GradientStops.Add(lggs);

                GradientStop ggs = new GradientStop();
                ggs.Color = Color.FromArgb(255, 108, 108, 108);
                ggs.Offset = 1.25;
                lgb.GradientStops.Add(ggs);

                listSP.Background = lgb;

                listSP.Orientation = Orientation.Horizontal;

                StackPanel reportSP = new StackPanel();

                queuedListIndex.Add(Que.ReportId);

                TextBlock ridTB = new TextBlock() { Text = Que.ReportId.ToString() };
                ridTB.Foreground = new SolidColorBrush(Colors.Black);
                reportSP.Children.Add(ridTB);

                TextBlock rtypeTB = new TextBlock() { Text = Que.ReportType };
                rtypeTB.Foreground = new SolidColorBrush(Colors.Black);
                reportSP.Children.Add(rtypeTB);

                TextBlock rtimeTB = new TextBlock() { Text = Que.ReportTime.ToString() };
                rtimeTB.Foreground = new SolidColorBrush(Colors.Black);
                reportSP.Children.Add(rtimeTB);

                ReportObject queuedReportObject = new ReportObject(Que.ReportId,
                                                                   Que.ReportType,
                                                                   Que.ReportAuthor,
                                                                   Que.ReportDescription,
                                                                   Que.ReportLocation,
                                                                   Que.ReportTime,
                                                                   Que.ReportLatitude,
                                                                   Que.ReportLongitude,
                                                                   Que.ReportAccuracy,
                                                                   Que.ReportDirection);

                queuedObjectList.Add(queuedReportObject);

                if (!(queuedReportObject.ReportLatitude == "NA" || queuedReportObject.ReportLongitude == "NA"))
                {
                    try
                    {
                        AddToMap(queuedReportObject);
                    }
                    catch (Exception) { }
                }

                image.Height = 110;
                image.Width = 175;

                ImageView.Content = image;
                ReportView.Content = reportSP;
                ImageView.AddHandler(UIElement.TappedEvent, new TappedEventHandler(QImageSelected), true);
                ReportView.AddHandler(UIElement.TappedEvent, new TappedEventHandler(QReportSelected), true);

                listSP.Children.Add(ReportView);
                listSP.Children.Add(ImageView);
                QueuedListView.Items.Add(listSP);
            }
            if (count > 0)
            {
                if (SIUC311.SettingsView.GetAutoSubmitSetting())
                {
                    await WaitablePromptMessage("You have " + count + " reports queued\n\nQueued reports will be automatically submitted.");
                }
                else
                {
                    await WaitablePromptMessage("You have " + count + " reports queued.");
                }
                if (haveInternetAccess)
                {
                    //NotifyUser("Processing queue . . . ", NotifyType.ReportMessage);
                    try
                    {
                        inSession = await SIU311Service.IsSessionAliveAsync(sessionId); // SLOW TAKES TIME 
                    }
                    catch (Exception)
                    {
                        inSession = false;
                    }
                    //NotifyUser("Session checked at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.ReportMessage);
                    if (inSession)
                    {
                        if (SIUC311.SettingsView.GetAutoSubmitSetting())
                        {
                            await SubmitQueue();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SubmitQueue()
        {
            bool success = false;
            if (await ConfirmSubmitQueue())
            {
                int remove_count = 0;
                //NotifyUser("Submitting queued reports. . .", NotifyType.StatusMessage);
                ControlsProgressBar.Visibility = Visibility.Visible;
                foreach (ReportObject qro in queuedObjectList)
                {
                    if (await SIU311Service.InsertReportAsync(sessionId, new SIUC311ServiceRef.ReportObject
                        {
                            ReportType = qro.ReportType,
                            ReportAuthor = qro.ReportAuthor,
                            ReportDescription = qro.ReportDescription,
                            ReportLocation = qro.ReportLocation,
                            ReportTime = qro.ReportTime,
                            ReportLatitude = qro.ReportLatitude,
                            ReportLongitude = qro.ReportLongitude,
                            ReportAccuracy = qro.ReportAccuracy,
                            ReportDirection = qro.ReportDirection
                        }))
                    {
                        //NotifyUser("Report Accepted at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.QueueMessage);

                        var file = await pictureFolder.GetFileAsync(((-qro.ReportId) - remove_count) + "_" + qro.ReportType + ".png");
                        byte[] photo = await ConvertImagetoByte(file);

                        if (await SIU311Service.InsertPhotoAsync(sessionId, new SIUC311ServiceRef.PhotoObject
                            {
                                ReportPhoto = photo
                            }))
                        {
                            //NotifyUser("Queued report " + qro.ReportId + " has been submitted.", NotifyType.ReportMessage);
                        }
                        else
                        {
                            //NotifyUser("Unable to upload photo", NotifyType.ReportMessage);
                            success = false;
                            break;
                        }
                    }
                    else
                    {
                        //NotifyUser("Report Rejected. Try Again.", NotifyType.ReportMessage);
                        success = false;
                        break;
                    }
                }
                if ((await RemoveQueue(false)))
                {
                    RefreshQueue();
                    ResetSession();
                    success = true;
                    //NotifyUser("Queue submitted at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.ReportMessage);                    
                }
                else
                {
                    await WaitablePromptMessage("Could not remove queue completely.");
                }
                ControlsProgressBar.Visibility = Visibility.Collapsed;
            }
            return success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RemoveQueue(bool confirm_remove)
        {
            bool queue_removed = false;
            bool remove_queue = false;

            if (confirm_remove)
            {
                remove_queue = await ConfirmRemoveQueue();
            }
            else
            {
                remove_queue = true;
            }

            if (remove_queue)
            {
                var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { ".txt" });
                var query = reportFolder.CreateFileQueryWithOptions(queryOptions);
                var report_files = await query.GetFilesAsync();
                queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { ".png" });
                query = pictureFolder.CreateFileQueryWithOptions(queryOptions);
                var picture_files = await query.GetFilesAsync();

                foreach (StorageFile rsf in report_files)
                {
                    try
                    {
                        await rsf.DeleteAsync(StorageDeleteOption.PermanentDelete);
                        queue_removed = true;
                    }
                    catch (Exception)
                    {
                        queue_removed = false;
                    }
                }
                foreach (StorageFile psf in picture_files)
                {
                    try
                    {
                        await psf.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                    catch (Exception)
                    {
                        queue_removed = false;
                    }
                }
            }
            return queue_removed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompt"></param>
        private async Task<bool> ConfirmRemoveQueue()
        {
            bool confirm_remove = false;
            if (messageDisplayed == false)
            {
                messageDisplayed = true;

                // Create the message dialog and set its content and title 
                var messageDialog = new MessageDialog("Do you want to remove all queued reports?", "SIU 311");

                // Add commands and set their callbacks 
                messageDialog.Commands.Add(new UICommand("YES", (command) =>
                {
                    messageDisplayed = false;
                    confirm_remove = true;
                }));

                // Add commands and set their callbacks 
                messageDialog.Commands.Add(new UICommand("NO", (command) =>
                {
                    messageDisplayed = false;
                }));

                // Set the command that will be invoked by default 
                messageDialog.DefaultCommandIndex = 1;

                try
                {
                    // Show the message dialog                 
                    await messageDialog.ShowAsync();
                }
                catch (Exception) { }
            }
            return confirm_remove;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompt"></param>
        private async Task<bool> ConfirmSubmitQueue()
        {
            bool confirm_submit = false;
            if (messageDisplayed == false)
            {
                messageDisplayed = true;

                // Create the message dialog and set its content and title 
                var messageDialog = new MessageDialog("Do you want to submit all queued reports?", "SIU 311");

                // Add commands and set their callbacks 
                messageDialog.Commands.Add(new UICommand("YES", (command) =>
                {
                    messageDisplayed = false;
                    confirm_submit = true;
                }));

                // Add commands and set their callbacks 
                messageDialog.Commands.Add(new UICommand("NO", (command) =>
                {
                    messageDisplayed = false;
                }));

                // Set the command that will be invoked by default 
                messageDialog.DefaultCommandIndex = 1;

                try
                {
                    // Show the message dialog                 
                    await messageDialog.ShowAsync();
                }
                catch (Exception) { }
            }
            return confirm_submit;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qro"></param>
        /// <returns></returns>
        private async Task<bool> SubmitQueuedReport(ReportObject qro)
        {
            bool success = true;
            if (await SIU311Service.InsertReportAsync(sessionId, new SIUC311ServiceRef.ReportObject
            {
                ReportType = qro.ReportType,
                ReportAuthor = qro.ReportAuthor,
                ReportDescription = qro.ReportDescription,
                ReportLocation = qro.ReportLocation,
                ReportTime = qro.ReportTime,
                ReportLatitude = qro.ReportLatitude,
                ReportLongitude = qro.ReportLongitude,
                ReportAccuracy = qro.ReportAccuracy,
                ReportDirection = qro.ReportDirection
            }))
            {
                //NotifyUser("Report Accepted at " + string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now), NotifyType.QueueMessage);

                //NotifyUser("Attempting to submit photo", NotifyType.StatusMessage);

                var file = await pictureFolder.GetFileAsync((-qro.ReportId) + "_" + qro.ReportType + ".png");

                byte[] photo = await ConvertImagetoByte(file);

                if (await SIU311Service.InsertPhotoAsync(sessionId, new SIUC311ServiceRef.PhotoObject
                {
                    ReportPhoto = photo
                }))
                {   
                    if (await RemoveQueuedReport(qro.ReportId))
                    {
                        await WaitablePromptMessage("Queued report " + qro.ReportId + " has been submitted and removed from local device.");
                    }
                    else
                    {
                        PromptMessage("Could not remove report " + qro.ReportId);
                        success = false;
                    }
                }
                else
                {
                    //NotifyUser("Unable to upload photo", NotifyType.ReportMessage);
                    success = false;
                }
            }
            else
            {
                //NotifyUser("Report Rejected. Try Again.", NotifyType.ReportMessage);
                success = false;
            }
            RefreshQueue();
            ResetSession();
            return success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QImageSelected(object sender, TappedRoutedEventArgs e)
        {
            if (QueuedListView.Items.Count > 0)
            {
                if (QueuedListView.SelectedIndex != queuedListIndex.IndexOf(Convert.ToInt32(((TextBlock)((StackPanel)((GridViewItem)((StackPanel)((GridViewItem)sender).Parent).Children[0]).Content).Children[0]).Text)))
                {
                    QueuedListView.SelectedIndex = queuedListIndex.IndexOf(Convert.ToInt32(((TextBlock)((StackPanel)((GridViewItem)((StackPanel)((GridViewItem)sender).Parent).Children[0]).Content).Children[0]).Text));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QReportSelected(object sender, TappedRoutedEventArgs e)
        {
            if (QueuedListView.Items.Count > 0)
            {
                if (QueuedListView.SelectedIndex != queuedListIndex.IndexOf(Convert.ToInt32(((TextBlock)((StackPanel)((GridViewItem)sender).Content).Children[0]).Text)))
                {
                    QueuedListView.SelectedIndex = queuedListIndex.IndexOf(Convert.ToInt32(((TextBlock)((StackPanel)((GridViewItem)sender).Content).Children[0]).Text));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void QueuedListView_Clicked(object sender, TappedRoutedEventArgs e)
        {
            if (QueuedListView.Items.Count > 0)
            {
                StackPanel rsp = (StackPanel)((GridViewItem)((StackPanel)((ListView)sender).SelectedValue).Children[0]).Content;
                await ShowReportPopup(((TextBlock)rsp.Children[0]).Text, true);
            }
            else
            {
                await WaitablePromptMessage("NO REPORTS");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private async Task<bool> RemoveQueuedReport(int n)
        {
            bool reportRemoved = false;
            bool photoRemoved = false;
            bool beginRename = false;

            int rn = -n;

            var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { ".txt" });
            var query = reportFolder.CreateFileQueryWithOptions(queryOptions);
            var report_files = await query.GetFilesAsync();

            queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { ".png" });
            query = pictureFolder.CreateFileQueryWithOptions(queryOptions);
            var picture_files = await query.GetFilesAsync();

            if ((queuedReportCount = report_files.Count) > 0)
            {
                int i = 0;
                foreach (StorageFile report_file in report_files)
                {
                    i++;
                    if (!reportRemoved)
                    {
                        if (report_file.Name == rn + "_" + QueuedReportType(n).Trim() + ".txt")
                        {                            
                            await report_file.DeleteAsync(StorageDeleteOption.Default);
                            reportRemoved = true;
                        }
                    }

                    if (beginRename && (queuedReportCount > 1))
                    {
                        await report_file.RenameAsync((i - 1) + "_" + QueuedReportType(-i).Trim() + ".txt");
                    }
                    if (reportRemoved)
                    {
                        beginRename = true;
                    }
                }

                i = 0;
                beginRename = false;
                foreach (StorageFile picture_file in picture_files)
                {
                    i++;
                    if (!photoRemoved)
                    {
                        if (picture_file.Name == rn + "_" + QueuedReportType(n).Trim() + ".png")
                        {
                            await picture_file.DeleteAsync(StorageDeleteOption.Default);
                            photoRemoved = true;
                        }
                    }

                    if (beginRename && (queuedReportCount > 1))
                    {
                        await picture_file.RenameAsync((i - 1) + "_" + QueuedReportType(-i).Trim() + ".png");
                    }
                    if (photoRemoved)
                    {
                        beginRename = true;
                    }
                }
            }

            return reportRemoved && photoRemoved;
        }
        #endregion

        #region Initialization
        /// <summary>
        /// sets textblocks for heading titles on the screen
        /// </summary>
        private void InitHeadings()
        {   
            ControlsBlock.Text = "Controls";
            ListBlock.Text = "Reports";
            MapBlock.Text = "Map";
            PhotoBlock.Text = "Photo";
            VideoBlock.Text = "Video";
            QueuedListBlock.Text = "Queue";

            //Form
            TypeBlock.Text = "Select type:";
            DescriptionBlock.Text = "Enter description:";
            LocationBlock.Text = "Enter location:";
        }

        /// <summary>
        /// Checks the default radio buttons for the button groups
        /// </summary>
        private void InitRadioButtons()
        {
            MyReportsRadioButton.IsChecked = true;
        }

        /// <summary>
        /// 
        /// </summary>
        async private void SetWindowsUser()
        {
            UserNameBlock.Text = await UserInformation.GetDisplayNameAsync();
            PopupUserNameBlock.Text = await UserInformation.GetDisplayNameAsync();
        }

        /// <summary> 
        ///
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        async private void SetDNSDomain()
        {
            if (!Windows.System.UserProfile.UserInformation.NameAccessAllowed)
            {
                //StatusBlock.Text = "Access to user information is disabled by the user or administrator";
                DomainNameBlock.Text = "Unknown";
                PopupDomainNameBlock.Text = "Unknown";
            }
            else
            {
                //StatusBlock.Text = "Beginning asynchronous operation.";
                String dns = await Windows.System.UserProfile.UserInformation.GetDomainNameAsync();
                if (String.IsNullOrEmpty(dns))
                {
                    // NULL may be returned in any of the following circumstances: 
                    // The information can not be retrieved from the directory 
                    // The calling user is not a member of a domain 
                    // The user or administrator has disabled the privacy setting 
                    //StatusBlock.Text = "No DNS domain name returned for the current user.";
                    DomainNameBlock.Text = "Unknown";
                    PopupDomainNameBlock.Text = "Unknown";
                }
                else
                {
                    //StatusBlock.Text = "Domain name returned for the current user.";
                    DomainNameBlock.Text = dns;
                    PopupDomainNameBlock.Text = dns;
                }
            }
        }
        #endregion

        #region Mapping
        /// <summary>
        /// 
        /// </summary>
        private void ClearMap()
        {
            buildingsAdded = false;

            Map.Children.Clear();

            Map.Children.Add(MapPopup);

            Map.ShapeLayers.Clear();
            MarkBuildings.Visibility = Visibility.Visible;
            Infobox.Visibility = Visibility.Collapsed;
            GeoPositionChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reportObject"></param>
        private void AddToMap(ReportObject reportObject)
        {
            Pushpin pushpin = new Pushpin();
            pushpin.Name = reportObject.ReportId.ToString();
            pushpin.Background = new SolidColorBrush(Color.FromArgb(255, 255, 165, 0));
            MapLayer.SetPosition(pushpin, new Location(Convert.ToDouble(reportObject.ReportLatitude), Convert.ToDouble(reportObject.ReportLongitude)));
            Canvas.SetZIndex(pushpin, 1);
            pushpin.Tapped += pushpinTapped;
            Map.Children.Add(pushpin);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reportObject"></param>
        private void AddToMap(Building buildingObject)
        {
            Pushpin pushpin = new Pushpin();
            pushpin.Name = "@ " + buildingObject.BuildingName;
            pushpin.DataContext = buildingObject.BuildingInformation + "|" + buildingObject.BuildingImageURL + "|" + buildingObject.BuildingLatitude + "|" + buildingObject.BuildingLongitude;
            pushpin.Background = new SolidColorBrush(Color.FromArgb(255, 102, 0, 0));
            MapLayer.SetPosition(pushpin, new Location(Convert.ToDouble(buildingObject.BuildingLatitude), Convert.ToDouble(buildingObject.BuildingLongitude)));
            Canvas.SetZIndex(pushpin, 1);
            pushpin.Tapped += pushpinTapped;
            Map.Children.Add(pushpin);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pushpinTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Pushpin tappedpin = sender as Pushpin;  // gets the pin that was tapped
            if (null == tappedpin) return;          // null check to prevent bad stuff if it wasn't a pin.

            var x = MapLayer.GetPosition(tappedpin);

            if (Map.ZoomLevel < 16.0)
            {
                Map.SetView(x, 16.0F);
            }
            else
            {
                if (e.GetPosition(Map).Y < 150 || e.GetPosition(Map).X < 100 || e.GetPosition(Map).X > Map.ActualWidth - 200)
                {
                    Map.SetView(x, Map.ZoomLevel);
                }
            }

            if (((Pushpin)sender).Name[0] == '@')
            {
                InfoBoxToReportButton.Visibility = Visibility.Collapsed;
                try
                {
                    await ShowMapBuildingInfoBox((Pushpin)sender);
                }
                catch (Exception)
                {
                    //QueuedBlock.Text = "ERROR DISPLAYING BUILDING INFOBOX";
                }
            }
            else
            {
                InfoBoxToReportButton.Visibility = Visibility.Visible;
                try
                {
                    await ShowMapReportInfoBox((Pushpin)sender);
                }
                catch (Exception)
                {
                    //QueuedBlock.Text = "ERROR DISPLAYING REPORT INFOBOX";
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <param name="queued"></param>
        /// <returns></returns>
        private async Task<bool> ShowMapReportInfoBox(Pushpin pin)
        {
            String number = pin.Name;
            selectedNumber = Convert.ToInt32(number);
            string details;
            if (selectedNumber < 0)
            {
                details = QueuedReportFullDescription(selectedNumber);
            }
            else
            {
                details = ReportFullDescription(selectedNumber);
            }

            string[] detail_split = details.Split(new char[] { '|' }, StringSplitOptions.None);

            try
            {
                selectedLatitude = Convert.ToDouble(detail_split[5]);
                selectedLongitude = Convert.ToDouble(detail_split[6]);
            }
            catch (Exception) { }

            Infobox.DataContext = number;

            Infobox.Visibility = Visibility.Visible;

            MapLayer.SetPosition(Infobox, MapLayer.GetPosition(pin));

            Infobox.Margin = new Thickness(-100, -200, 0, 0);

            Canvas.SetZIndex(MapPopup, 2);

            if (selectedNumber < 0)
            {
                selectedIsQueued = true;
                if (QueuedListView.Items.Count > 0)
                {
                    InfoBoxTitle.Text = "Queued Report " + number;
                    InfoBoxDetails.Text = detail_split[0] + "\n" +
                                          detail_split[4] + "\n" +
                                          detail_split[3] + "\n" +
                                          detail_split[8];

                    var file = await pictureFolder.GetFileAsync(detail_split[1].Substring(1) + "_" + detail_split[0] + ".png");

                    var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    var img = new BitmapImage();
                    img.SetSource(fileStream);

                    InfoBoxPhoto.Source = img;
                }
                else { }
            }
            else
            {
                selectedIsQueued = false;
                if (simpleQuery == 0)
                {
                    if (detail_split[2] == UserNameBlock.Text)
                    {
                    }
                    else
                    {
                        if (isAdmin)
                        {
                        }
                    }
                }
                else
                {
                }

                SIUC311.SIUC311ServiceRef.ReportManagement rmo = null;

                if (isAdmin)
                {
                    try
                    {
                        rmo = await SIU311Service.GetReportManagementAsync(selectedNumber);

                        if (rmo.ReportPriority == "Normal")
                        {
                        }
                        else
                        {
                        }

                        if (rmo.ReportStatus == "Closed")
                        {
                        }
                        else
                        {
                        }
                    }
                    catch (Exception) { }
                }

                if (ReportsListView.Items.Count > 0)
                {
                    InfoBoxTitle.Text = "Report " + number;
                    InfoBoxDetails.Text = detail_split[0] + "\n" +
                                          detail_split[4] + "\n" +
                                          detail_split[3] + "\n" +
                                          detail_split[8];

                    var photoObject = await SIU311Service.GetPhotoAsync(Convert.ToInt32(detail_split[1]));

                    BitmapImage bimage;

                    if (photoObject != null)
                    {
                        bimage = await ByteToImage(photoObject.ReportPhoto);
                    }
                    else
                    {
                        bimage = new BitmapImage(new Uri("ms-appx:///Assets/ImagePlaceHolder.png"));
                    }

                    InfoBoxPhoto.Source = bimage;

                    if (rmo != null)
                    {
                        //ReportStatus.Text = rmo.ReportStatus;
                        //ReportPriority.Text = rmo.ReportPriority;
                    }
                }
                else { }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pin"></param>
        /// <returns></returns>
        private async Task<bool> ShowMapBuildingInfoBox(Pushpin pin)
        {
            InfoBoxTitle.Text = pin.Name.Substring(1);

            string[] data_split = pin.DataContext.ToString().Split(new char[] { '|' }, StringSplitOptions.None);

            InfoBoxDetails.Text = data_split[0];

            selectedLatitude = Convert.ToDouble(data_split[2]);
            selectedLongitude = Convert.ToDouble(data_split[3]);

            if ((selectedLatitude > 0.0 && selectedLongitude > 0.0))
            {

            }

            Uri uri = null;
            if (!data_split[1].Contains("9999.jpg"))
            {
                try
                {
                    uri = new Uri(data_split[1]);
                }
                catch (Exception) { }
            }

            var fileName = Guid.NewGuid().ToString() + ".jpg";

            var bitmapImage = new BitmapImage();
            var httpClient = new HttpClient();
            byte[] b = null;
            if (uri != null)
            {
                try
                {
                    var httpResponse = await httpClient.GetAsync(uri);
                    b = await httpResponse.Content.ReadAsByteArrayAsync();
                }
                catch (Exception) { }
            }

            BitmapImage bimage;

            if (b != null)
            {
                bimage = await ByteToImage(b);
            }
            else
            {
                bimage = new BitmapImage(new Uri("ms-appx:///Assets/ImagePlaceHolder.png"));
            }

            InfoBoxPhoto.Source = bimage;
            
            Infobox.Visibility = Visibility.Visible;

            MapLayer.SetPosition(Infobox, MapLayer.GetPosition(pin));

            Infobox.Margin = new Thickness(-100, -200, 0, 0);

            Canvas.SetZIndex(MapPopup, 2);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void InfoboxToReport_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            await ShowReportPopup(selectedNumber.ToString(), selectedIsQueued);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void InfoboxToRoute_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (haveInternetAccess)
            {
                string xml_route = null;

                try
                {
                    HttpClient routeClient;
                    routeClient = new HttpClient();
                    routeClient.MaxResponseContentBufferSize = 256000;

                    HttpResponseMessage response = await routeClient.GetAsync("http://dev.virtualearth.net/REST/V1/Routes/Walking?wp.0=" + LatitudeTextblock.Text + "," + LongitudeTextblock.Text + "&wp.1=" + selectedLatitude + "," + selectedLongitude + "&optmz=distance&output=xml&key=" + Constants.bing_key);

                    response.EnsureSuccessStatusCode();

                    xml_route = await response.Content.ReadAsStringAsync();

                }
                catch (Exception)
                {
                    //QueuedBlock.Text = "ERROR GETTING ROUTE";
                }

                DrawRoute(ParseRoute(xml_route));
            }
            else
            {
                await WaitablePromptMessage("No internet connection found. Unable to obtain route.\n\nPlease connect to the internet and try again.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        private List<Location> ParseRoute(string route)
        {
            List<Location> point_list = new List<Location>();
            Location temp = new Location();
            Location end = new Location();

            route = route.Substring(route.IndexOf("<ActualStart>"));
            temp.Latitude = Convert.ToDouble(route.Substring(route.IndexOf("<Latitude>") + 10, route.IndexOf("</") - (route.IndexOf("<Latitude>") + 10)));
            route = route.Substring(route.IndexOf("</Latitude>") + 11);
            temp.Longitude = Convert.ToDouble(route.Substring(route.IndexOf("<Longitude>") + 11, route.IndexOf("</") - (route.IndexOf("<Longitude>") + 11)));

            point_list.Add(temp);

            route = route.Substring(route.IndexOf("<ActualEnd>"));
            end.Latitude = Convert.ToDouble(route.Substring(route.IndexOf("<Latitude>") + 10, route.IndexOf("</") - (route.IndexOf("<Latitude>") + 10)));
            route = route.Substring(route.IndexOf("</Latitude>") + 11);
            end.Longitude = Convert.ToDouble(route.Substring(route.IndexOf("<Longitude>") + 11, route.IndexOf("</") - (route.IndexOf("<Longitude>") + 11)));

            while (route.IndexOf("<ManeuverPoint>") > -1)
            {
                temp = new Location();
                route = route.Substring(route.IndexOf("<ManeuverPoint>"));
                temp.Latitude = Convert.ToDouble(route.Substring(route.IndexOf("<Latitude>") + 10, route.IndexOf("</") - (route.IndexOf("<Latitude>") + 10)));
                route = route.Substring(route.IndexOf("</Latitude>") + 11);
                temp.Longitude = Convert.ToDouble(route.Substring(route.IndexOf("<Longitude>") + 11, route.IndexOf("</") - (route.IndexOf("<Longitude>") + 11)));

                point_list.Add(temp);
            }

            point_list.Add(end);

            return point_list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        private void DrawRoute(List<Location> points)
        {
            MapShapeLayer shapeLayer = new MapShapeLayer();
            MapPolyline polyline = new MapPolyline();

            LocationCollection lc = new LocationCollection();

            foreach (Location p in points)
            {
                lc.Add(p);
            }

            polyline.Locations = lc;
            polyline.Color = Color.FromArgb(255, 102, 0, 0);
            polyline.Width = 5;
            shapeLayer.Shapes.Add(polyline);
            Map.ShapeLayers.Clear();
            Map.ShapeLayers.Add(shapeLayer);

            Location midpoint = new Location(lc.FirstOrDefault().Latitude + ((lc.LastOrDefault().Latitude - lc.FirstOrDefault().Latitude) / 2.0),
                                             lc.FirstOrDefault().Longitude + ((lc.LastOrDefault().Longitude - lc.FirstOrDefault().Longitude) / 2.0));

            Map.SetView(midpoint, 15.5F);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseInfobox_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Infobox.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Report Popup
        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <param name="queued"></param>
        /// <returns></returns>
        private async Task<bool> ShowReportPopup(String number, bool queued)
        {
            // close the Popup if it open already
            if (ReportPopup.IsOpen) { ReportPopup.IsOpen = false; }

            ReportPopup.IsOpen = true;

            selectedIsQueued = queued;
            selectedNumber = Convert.ToInt32(number);
            string details;
            if (queued)
            {
                details = QueuedReportFullDescription(selectedNumber);
                RemoveQueuedReportPopupButton.Visibility = Visibility.Visible;
            }
            else
            {
                details = ReportFullDescription(selectedNumber);
                RemoveQueuedReportPopupButton.Visibility = Visibility.Collapsed;
            }

            string[] detail_split = details.Split(new char[] { '|' }, StringSplitOptions.None);

            if (queued)
            {
                if (inSession)
                {
                    SubmitQueuedReportPopupButton.Visibility = Visibility.Visible;
                }
                else
                {
                    SubmitQueuedReportPopupButton.Visibility = Visibility.Collapsed;
                }
                RemoveReportPopupButton.Visibility = Visibility.Collapsed;
                RemoveQueuedReportPopupButton.Visibility = Visibility.Visible;
                if (QueuedListView.Items.Count > 0)
                {
                    ReportPopupTitle.Text = "Queued Report " + number;
                    ReportType.Text = detail_split[0];
                    ReportLocation.Text = detail_split[4];
                    ReportDescription.Text = detail_split[3];
                    ReportAuthor.Text = detail_split[2];
                    ReportTime.Text = detail_split[8];

                    var file = await pictureFolder.GetFileAsync(detail_split[1].Substring(1) + "_" + detail_split[0] + ".png");

                    var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    var img = new BitmapImage();
                    img.SetSource(fileStream);

                    ReportPhoto.Source = img;

                    ReportStatus.Text = "Not Submitted";
                }
                else { }
            }
            else
            {
                SubmitQueuedReportPopupButton.Visibility = Visibility.Collapsed;
                bool removable = false;
                if (simpleQuery == 0)
                {
                    if (detail_split[2] == UserNameBlock.Text)
                    {
                        removable = true;
                        ClaimReportButton.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        if (isAdmin)
                        {
                            ClaimReportButton.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    removable = true;
                }

                if (removable)
                {
                    if (queued)
                    {
                        RemoveReportPopupButton.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        RemoveReportPopupButton.Visibility = Visibility.Visible;
                        RemoveQueuedReportPopupButton.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    RemoveReportPopupButton.Visibility = Visibility.Collapsed;
                }

                SIUC311.SIUC311ServiceRef.ReportManagement rmo = null;

                if (isAdmin)
                {
                    try
                    {
                        rmo = await SIU311Service.GetReportManagementAsync(selectedNumber);

                        if (rmo.ReportPriority == "Normal")
                        {
                            ElevateReportButton.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ElevateReportButton.Visibility = Visibility.Collapsed;
                        }

                        if (rmo.ReportStatus == "Closed")
                        {
                            CloseReportButton.Visibility = Visibility.Collapsed;
                            OpenReportButton.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            OpenReportButton.Visibility = Visibility.Collapsed;
                            CloseReportButton.Visibility = Visibility.Visible;
                        }
                    }
                    catch (Exception) { }
                }

                if (ReportsListView.Items.Count > 0)
                {
                    ReportPopupTitle.Text = "Report " + number;
                    ReportType.Text = detail_split[0];
                    ReportLocation.Text = detail_split[4];
                    ReportDescription.Text = detail_split[3];
                    ReportAuthor.Text = detail_split[2];
                    ReportTime.Text = detail_split[8];

                    var photoObject = await SIU311Service.GetPhotoAsync(Convert.ToInt32(detail_split[1]));

                    BitmapImage bimage;

                    if (photoObject != null)
                    {
                        bimage = await ByteToImage(photoObject.ReportPhoto);
                    }
                    else
                    {
                        bimage = new BitmapImage(new Uri("ms-appx:///Assets/ImagePlaceHolder.png"));
                    }

                    ReportPhoto.Source = bimage;

                    if (rmo != null)
                    {
                        ReportStatus.Text = rmo.ReportStatus;
                        ReportPriority.Text = rmo.ReportPriority;
                    }
                }
                else { }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public async void ConditionalForwardReport()
        {
            if (page <= lastPage)
            {
                if (reportListIndex.IndexOf(selectedNumber) + 1 < reportListIndex.Count)
                {
                    await ShowReportPopup((reportObjectList[reportListIndex.IndexOf(selectedNumber) + 1].ReportId).ToString(), false);
                }
                else
                {
                    if (Next.Visibility == Visibility.Visible)
                    {
                        ButtonAutomationPeer peer = new ButtonAutomationPeer(Next);
                        peer.Invoke();
                        ReportPopupProgressBar.Visibility = Visibility.Visible;
                        await Task.Delay(1000);
                        await ShowReportPopup((reportObjectList[0].ReportId).ToString(), false);
                        ReportPopupProgressBar.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        NotifyUser("END OF REPORTS", SIUC311.NotifyType.QueueMessage);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public async void ConditionalBackwardReport()
        {
            if (page >= 0)
            {
                if (reportListIndex.IndexOf(selectedNumber) - 1 >= 0)
                {
                    await ShowReportPopup((reportObjectList[reportListIndex.IndexOf(selectedNumber) - 1].ReportId).ToString(), false);
                }
                else if (reportListIndex.IndexOf(selectedNumber) - 1 < 0)
                {
                    if (Previous.Visibility == Visibility.Visible)
                    {
                        ButtonAutomationPeer peer = new ButtonAutomationPeer(Previous);
                        peer.Invoke();
                        ReportPopupProgressBar.Visibility = Visibility.Visible;
                        await Task.Delay(5000);
                        await ShowReportPopup((reportObjectList[reportObjectList.Count - 1].ReportId).ToString(), false);
                        ReportPopupProgressBar.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        NotifyUser("BEGINNING OF REPORTS", SIUC311.NotifyType.QueueMessage);
                    }
                }
            }
        }
        #endregion

        #region Resets
        /// <summary>
        /// clears the reports list displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearReports()
        {
            ReportsListView.Items.Clear();
            NotifyUser("No reports displayed", NotifyType.DisplayMessage);
            Next.Visibility = Visibility.Collapsed;
            Previous.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// clears the description, photo captured, video captured
        /// returns combobox to default
        /// </summary>
        private void ResetReportForm()
        {
            GetGeolocation();
            TypeCombobox.SelectedIndex = 1;
            DescriptionTextbox.Text = "";
            LocationTextbox.Text = "";

            CapturedPhoto.Source = new BitmapImage(new Uri("ms-appx:///Assets/ImagePlaceHolder.png"));
            ReportFormPhoto.Source = new BitmapImage(new Uri("ms-appx:///Assets/ImagePlaceHolder.png"));
            CapturedVideo.Source = null;

            havePhoto = false;
            haveVideo = false;

            appSettings.Remove(Constants.videoKey);
            appSettings.Remove(Constants.photoKey);
        }

        /// <summary>
        /// Loads the video from file path
        /// </summary>
        /// <param name="filePath">The path to load the video from</param>
        private async Task ReloadVideo(String filePath)        
        {
            CapturedVideo.Visibility = Visibility.Visible;
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                CapturedVideo.SetSource(fileStream, "video/mp4");

                //NotifyUser("Video reloaded", NotifyType.StatusMessage);
            }
            catch (Exception)
            {
                appSettings.Remove(Constants.videoKey);
                //NotifyUser("ERROR LOADING VIDEO", NotifyType.ErrorMessage);
            }
        }
        #endregion

        #region Confirmations
        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        private async Task<bool> ConfirmRemoveMessage(int number, bool queued)
        {
            bool remove_report = false;
            if (messageDisplayed == false)
            {
                messageDisplayed = true;

                if (queued)
                {
                    if (QueuedListView.Items.Count > 0)
                    {
                        // Create the message dialog and set its content and title 
                        var messageDialog = new MessageDialog("Are you sure you want to remove report " + number + "?", "Remove Report " + number);

                        // Add commands and set their callbacks 
                        messageDialog.Commands.Add(new UICommand("YES", (command) =>
                        {
                            remove_report = true;
                            messageDisplayed = false;
                        }));

                        messageDialog.Commands.Add(new UICommand("NO", (command) =>
                        {
                            messageDisplayed = false;
                        }));

                        // Set the command that will be invoked by default 
                        messageDialog.DefaultCommandIndex = 1;

                        // Show the message dialog 
                        await messageDialog.ShowAsync();
                    }
                    else
                    {
                        // Display default string.
                    }
                }
                else
                {
                    if (ReportsListView.Items.Count > 0)
                    {
                        // Create the message dialog and set its content and title 
                        var messageDialog = new MessageDialog("Are you sure you want to remove report " + number + "?", "Remove Report " + number);

                        // Add commands and set their callbacks 
                        messageDialog.Commands.Add(new UICommand("YES", (command) =>
                        {
                            remove_report = true;
                            messageDisplayed = false;
                        }));

                        messageDialog.Commands.Add(new UICommand("NO", (command) =>
                        {
                            messageDisplayed = false;
                        }));

                        // Set the command that will be invoked by default 
                        messageDialog.DefaultCommandIndex = 1;

                        // Show the message dialog 
                        await messageDialog.ShowAsync();
                    }
                    else
                    {
                        // Display default string.
                    }
                }
            }
            return remove_report;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        private async Task<bool> ConfirmOpenCloseReport(int number, bool isClosing)
        {
            bool report_modifier = false;
            if (messageDisplayed == false)
            {
                messageDisplayed = true;

                if (isClosing)
                {

                    // Create the message dialog and set its content and title 
                    var messageDialog = new MessageDialog("Are you sure you want to close report " + number + "?", "Close Report " + number);

                    // Add commands and set their callbacks 
                    messageDialog.Commands.Add(new UICommand("YES", (command) =>
                    {
                        report_modifier = true;
                        messageDisplayed = false;
                    }));

                    messageDialog.Commands.Add(new UICommand("NO", (command) =>
                    {
                        messageDisplayed = false;
                    }));

                    // Set the command that will be invoked by default 
                    messageDialog.DefaultCommandIndex = 1;

                    // Show the message dialog 
                    await messageDialog.ShowAsync();

                }
                else
                {

                    // Create the message dialog and set its content and title 
                    var messageDialog = new MessageDialog("Are you sure you want to open report " + number + "?", "Open Report " + number);

                    // Add commands and set their callbacks 
                    messageDialog.Commands.Add(new UICommand("YES", (command) =>
                    {
                        report_modifier = true;
                        messageDisplayed = false;
                    }));

                    messageDialog.Commands.Add(new UICommand("NO", (command) =>
                    {
                        messageDisplayed = false;
                    }));

                    // Set the command that will be invoked by default 
                    messageDialog.DefaultCommandIndex = 1;

                    // Show the message dialog 
                    await messageDialog.ShowAsync();

                }
            }
            return report_modifier;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        private async Task<bool> ConfirmElevateReport(int number)
        {
            bool report_modifier = false;
            if (messageDisplayed == false)
            {
                messageDisplayed = true;

                // Create the message dialog and set its content and title 
                var messageDialog = new MessageDialog("Are you sure you want to make report " + number + " urgent?", "Elevate Report " + number);

                // Add commands and set their callbacks 
                messageDialog.Commands.Add(new UICommand("YES", (command) =>
                {
                    report_modifier = true;
                    messageDisplayed = false;
                }));

                messageDialog.Commands.Add(new UICommand("NO", (command) =>
                {
                    messageDisplayed = false;
                }));

                // Set the command that will be invoked by default 
                messageDialog.DefaultCommandIndex = 1;

                // Show the message dialog 
                await messageDialog.ShowAsync();
            }
            return report_modifier;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        private async Task<bool> ConfirmClaimReport(int number)
        {
            bool report_modifier = false;
            if (messageDisplayed == false)
            {
                messageDisplayed = true;

                // Create the message dialog and set its content and title 
                var messageDialog = new MessageDialog("Are you sure you want to take ownership of report " + number + "?", "Claim Report " + number);

                // Add commands and set their callbacks 
                messageDialog.Commands.Add(new UICommand("YES", (command) =>
                {
                    report_modifier = true;
                    messageDisplayed = false;
                }));

                messageDialog.Commands.Add(new UICommand("NO", (command) =>
                {
                    messageDisplayed = false;
                }));

                // Set the command that will be invoked by default 
                messageDialog.DefaultCommandIndex = 1;

                // Show the message dialog 
                await messageDialog.ShowAsync();
            }
            return report_modifier;
        }
        #endregion

        #region Prompts
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompt"></param>
        internal async void PromptMessage(String prompt)
        {
            if (messageDisplayed == false)
            {
                messageDisplayed = true;

                // Create the message dialog and set its content and title 
                var messageDialog = new MessageDialog(prompt, "SIU 311");

                // Add commands and set their callbacks 
                messageDialog.Commands.Add(new UICommand("OK", (command) =>
                {
                    messageDisplayed = false;
                }));

                // Set the command that will be invoked by default 
                messageDialog.DefaultCommandIndex = 1;

                // Show the message dialog 
                await messageDialog.ShowAsync();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompt"></param>
        private async Task WaitablePromptMessage(String prompt)
        {
            if (messageDisplayed == false)
            {
                messageDisplayed = true;

                // Create the message dialog and set its content and title 
                var messageDialog = new MessageDialog(prompt, "SIU 311");

                // Add commands and set their callbacks 
                messageDialog.Commands.Add(new UICommand("OK", (command) =>
                {
                    messageDisplayed = false;
                }));

                // Set the command that will be invoked by default 
                messageDialog.DefaultCommandIndex = 1;

                try
                {
                    // Show the message dialog                 
                    await messageDialog.ShowAsync();
                }
                catch (Exception) { }
            }
        }
        #endregion

        #region GPS
        /// <summary>
        /// Invoked from OnPositionChanged and MapRadioButton_Checked
        /// 
        /// if not positioning clear map layer, get geoposition, 
        /// zoom to location, draws beacon on map layer
        /// 
        /// uses _geolocatorForMap
        /// </summary>
        /// <param></param>
        async private void GeoPositionChanged()
        {
            if (!positioning)
            {
                positioning = true;
                if (Map.Visibility == Visibility.Visible)
                {
                    if (updatedListForMap == false)
                    {
                        // Remove any previous location icon.
                        if (Map.Children.Count > 0)
                        {
                            Map.Children.RemoveAt(0);
                        }
                    }
                }

                try
                {
                    // Get the cancellation token.
                    _cts = new CancellationTokenSource();
                    CancellationToken token = _cts.Token;

                    //NotifyUser("Waiting for update...", NotifyType.StatusMessage);
                    // Get the location.
                    Geoposition pos = await _geolocatorForMap.GetGeopositionAsync().AsTask(token);

                    Location location = new Location(pos.Coordinate.Latitude, pos.Coordinate.Longitude);
                    if (Map.Visibility == Visibility.Visible)
                    {
                        // Now set the zoom level of the map based on the accuracy of our location data.
                        // Default to IP level accuracy. We only show the region at this level - No icon is displayed.
                        double zoomLevel = 13.0f;

                        // if we have GPS level accuracy
                        if (pos.Coordinate.Accuracy <= 10)
                        {
                            // Add the 10m icon and zoom closer.
                            Map.Children.Add(_locationIcon10m);
                            MapLayer.SetPosition(_locationIcon10m, location);
                            zoomLevel = 16.0f;
                        }
                        // Else if we have Wi-Fi level accuracy.
                        else if (pos.Coordinate.Accuracy <= 100)
                        {
                            // Add the 100m icon and zoom a little closer.
                            Map.Children.Add(_locationIcon100m);
                            MapLayer.SetPosition(_locationIcon100m, location);
                            zoomLevel = 15.0f;
                        }
                        // Else if we have unknown accuracy.
                        else if (pos.Coordinate.Accuracy <= 2000)
                        {
                            Map.Children.Add(_locationIcon2000m);
                            MapLayer.SetPosition(_locationIcon2000m, location);
                            zoomLevel = 14.0f;
                        }

                        // Set the map to the given location and zoom level.
                        Map.SetView(location, zoomLevel);
                    }
                    // Display the location information in the textboxes.                
                    String time = string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now);
                    NotifyUser("Welcome to SIU 311", NotifyType.DisplayMessage);
                    //NotifyUser("Location last updated " + time, NotifyType.StatusMessage);
                    UpdateTime(time);
                    UpdateGeolocation(pos.Coordinate.Latitude.ToString(),
                                      pos.Coordinate.Longitude.ToString(),
                                      pos.Coordinate.Accuracy.ToString());

                    if (updatedListForMap == false)
                    {
                        updatedListForMap = true;
                        foreach (var report in reportObjectList)
                        {
                            try
                            {
                                AddToMap(report);
                            }
                            catch (Exception) { }
                        }
                    }

                    positioning = false;
                }
                catch (System.UnauthorizedAccessException)
                {
                    //NotifyUser("Disabled", NotifyType.StatusMessage);
                    UpdateGeolocation("No data", "No data", "No data");
                    positioning = false;
                }
                catch (TaskCanceledException)
                {
                    //NotifyUser("Canceled", NotifyType.StatusMessage);
                    positioning = false;
                }
                catch (Exception)
                {
                    positioning = false;
                }
                finally
                {
                    _cts = null;
                    positioning = false;
                }
            }
        }

        /// <summary>
        /// Invoked from CapturePhoto_Click, CaptureVideo_Click, and ResetReportForm
        /// 
        /// updates position
        /// 
        /// uses _geolocatorForRequest
        /// </summary>
        /// <param></param>
        async private void GetGeolocation()
        {
            try
            {
                // Get cancellation token
                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;

                //NotifyUser("Waiting for location...", NotifyType.StatusMessage);

                // Carry out the operation
                Geoposition pos = await _geolocatorForRequest.GetGeopositionAsync().AsTask(token);

                String time = string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now);
                //NotifyUser("Location last updated " + time, NotifyType.StatusMessage);
                UpdateTime(time);
                UpdateGeolocation(pos.Coordinate.Latitude.ToString(),
                                  pos.Coordinate.Longitude.ToString(),
                                  pos.Coordinate.Accuracy.ToString());
            }
            catch (System.UnauthorizedAccessException)
            {
                //NotifyUser("Disabled", NotifyType.StatusMessage);
                UpdateGeolocation("No data", "No data", "No data");
            }
            catch (TaskCanceledException)
            {
                //NotifyUser("Canceled", NotifyType.StatusMessage);
            }
            catch (Exception)
            {
            }
            finally
            {
                _cts = null;
            }
        }
        #endregion

        #region Handlers
        /// <summary> 
        /// This is the event handler for PositionChanged events. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        async private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Geoposition pos = e.Position;
                String time = string.Format("{0:M/d/yyyy H:mm:ss tt}", DateTime.Now);
                //NotifyUser("Location last updated " + time, NotifyType.StatusMessage);
                UpdateTime(time);
                UpdateGeolocation(pos.Coordinate.Latitude.ToString(),
                                  pos.Coordinate.Longitude.ToString(),
                                  pos.Coordinate.Accuracy.ToString());
                GeoPositionChanged();
            });
        }

        /// <summary>
        /// compass handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async private void CompassReadingChanged(object sender, CompassReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                CompassReading reading = e.Reading;
                //DirectionTextblock.Text = reading.HeadingMagneticNorth.ToString().Trim();// String.Format("{0,5:0.00}", reading.HeadingMagneticNorth).Trim();

                if (reading.HeadingTrueNorth != null)
                {
                    //DirectionTextblock.Text = reading.HeadingTrueNorth.ToString().Trim();// String.Format("{0,5:0.00}", reading.HeadingTrueNorth).Trim();
                }
            });
        }

        /// <summary>
        /// inclinometer handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async void InclinometerReadingChanged(Inclinometer sender, InclinometerReadingChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                InclinometerReading reading = args.Reading;
                var pitchDouble = reading.PitchDegrees;
                var rollDouble = reading.RollDegrees;
                var yawDouble = reading.YawDegrees;
                int pitch = Convert.ToInt32(pitchDouble);
                int roll = Convert.ToInt32(rollDouble);
                int yaw = Convert.ToInt32(yawDouble);
                //Pitch.Text = pitch.ToString();
                //Roll.Text = roll.ToString();
                //Yaw.Text = yaw.ToString();

                //Vector.Text = GetCameraViewDirection(pitch, roll, yaw).ToString();
                DirectionTextblock.Text = GetCameraViewDirection(pitch, roll, yaw).ToString();
            });
        }

        /// <summary>
        /// Gives direction vector of the camera
        /// 
        /// straight up   -1
        /// straight down -1
        /// north         0 or 360
        /// south         180
        /// east          90
        /// west          270
        /// </summary>
        /// <param name="pitch"></param>
        /// <param name="roll"></param>
        /// <param name="yaw"></param>
        /// <returns></returns>
        public int GetCameraViewDirection(int pitch, int roll, int yaw)
        {
            if (roll < -20 && roll > -160)
            {
                return Normalize(360 - yaw + 90);
            }
            else if (roll > 20 && roll < 160)
            {
                return Normalize(360 - yaw - 90);
            }
            else if (pitch > 20 && pitch < 160)
            {
                return Normalize(-yaw);
            }
            else if (pitch < -20 && pitch > -160)
            {
                return Normalize(360 - yaw + 180);
            }

            // No sensible data
            return -1;
        }

        /// <summary>
        /// Normalizes compass direction
        /// </summary>
        /// <param name="compassDirection"></param>
        /// <returns></returns>
        private int Normalize(int compassDirection)
        {
            if (compassDirection > 360)
            {
                compassDirection -= 360;
            }
            if (compassDirection < 0)
            {
                compassDirection += 360;
            }
            return compassDirection;
        }
        #endregion

        #region Manipulations
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private String ReportFullDescription(int i)
        {
            foreach (var rli in reportObjectList)
            {
                if (rli.ReportId == i)
                    return rli.ReportType + "|" +
                           rli.ReportId + "|" +
                           rli.ReportAuthor + "|" +
                           rli.ReportDescription + "|" +
                           rli.ReportLocation + "|" +
                           rli.ReportLatitude + "|" +
                           rli.ReportLongitude + "|" +
                           rli.ReportDirection + "|" +
                           rli.ReportTime;
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private String QueuedReportFullDescription(int i)
        {
            foreach (var rli in queuedObjectList)
            {
                if (rli.ReportId == i)
                    return rli.ReportType + "|" +
                           rli.ReportId + "|" +
                           rli.ReportAuthor + "|" +
                           rli.ReportDescription + "|" +
                           rli.ReportLocation + "|" +
                           rli.ReportLatitude + "|" +
                           rli.ReportLongitude + "|" +
                           rli.ReportDirection + "|" +
                           rli.ReportTime;
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private String QueuedReportType(int i)
        {
            foreach (var rli in queuedObjectList)
            {
                if (rli.ReportId == i)
                    return rli.ReportType;
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public String TypeFromIndex(int i)
        {
            switch (i)
            {
                case 0: return "Pothole";
                case 1: return "Broken parking meter";
                case 2: return "Stolen property";
                case 3: return "Stray animal";
                case 4: return "Animal Bite";
                case 5: return "Garbage Collection";
                case 6: return "Abandon automobile";
                case 7: return "Abandon property";
                case 8: return "Illegal parking";
                case 9: return "Illegal dumping";
                case 10: return "Street light repair";
                case 11: return "Tree services";
                case 12: return "Plumbing";
                case 13: return "Trash";
                case 14: return "Building damages";
                case 15: return "Graffiti";
                case 16: return "Sidewalk condition";
                case 17: return "Lost property";
                case 18: return "Dangerous weather";
                case 19: return "Other";
                default: return "Default";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sdt"></param>
        /// <returns></returns>
        private DateTime ConvertToDateTime(String sdt)
        {
            string[] dt_split = sdt.Split(new char[] { ' ' }, StringSplitOptions.None);
            string[] d_split = dt_split[0].Split(new char[] { '/' }, StringSplitOptions.None);
            string[] t_split = dt_split[1].Split(new char[] { ':' }, StringSplitOptions.None);
            if (dt_split[2] == "PM")
            {
                return new DateTime(Convert.ToInt32(d_split[2]),
                                   Convert.ToInt32(d_split[0]),
                                   Convert.ToInt32(d_split[1]),
                                   Convert.ToInt32(t_split[0]) + 12,
                                   Convert.ToInt32(t_split[1]),
                                   Convert.ToInt32(t_split[2]));
            }
            else
            {
                return new DateTime(Convert.ToInt32(d_split[2]),
                                   Convert.ToInt32(d_split[0]),
                                   Convert.ToInt32(d_split[1]),
                                   Convert.ToInt32(t_split[0]),
                                   Convert.ToInt32(t_split[1]),
                                   Convert.ToInt32(t_split[2]));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private async Task<byte[]> ConvertImagetoByte(StorageFile image)
        {
            IRandomAccessStream fileStream = await image.OpenAsync(FileAccessMode.Read);
            var reader = new Windows.Storage.Streams.DataReader(fileStream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)fileStream.Size);

            byte[] pixels = new byte[fileStream.Size];

            reader.ReadBytes(pixels);

            return pixels;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <returns></returns>
        private static async Task<BitmapImage> ByteToImage(byte[] imageBytes)
        {
            MemoryStream stream = new MemoryStream(imageBytes);
            var randomAccessStream = new MemoryRandomAccessStream(stream);
            BitmapImage bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(randomAccessStream);
            return bitmapImage;
        }
        #endregion

        #region Controls
        /// <summary>
        /// handler for radio button for query all reports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueryAllReports_Checked(object sender, RoutedEventArgs e)
        {
            AllReportsRadioButton.IsChecked = true;
            isQueryAllNew = true;
            SortByAuthorCheckBox.IsEnabled = true;
            SortByAuthorSelectGroupTextblock.Foreground = new SolidColorBrush(Colors.Black);
            simpleQuery = 0;
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// handler for radio button for query my reports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueryMyReports_Checked(object sender, RoutedEventArgs e)
        {
            MyReportsRadioButton.IsChecked = true;
            isQueryMyNew = true;
            SortByAuthorCheckBox.IsEnabled = false;
            SortByAuthorSelectGroupTextblock.Foreground = new SolidColorBrush(Colors.Gray);
            simpleQuery = 1;
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// handler for checking check box to choose to query by type
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueryReportsByType_Checked(object sender, RoutedEventArgs e)
        {
            queryByType = true;
            ReportTypeCombobox.Visibility = Visibility.Visible;
            SortByTypeCheckBox.IsEnabled = false;
            SortByTypeSelectGroupTextblock.Foreground = new SolidColorBrush(Colors.Gray);

            queryReportType = TypeFromIndex(1).Trim();
            ReportTypeCombobox.SelectedIndex = 1;

            isQueryAllByTypeNew = true;
            isQueryMyByTypeNew = true;
            isQueryPaged = false;

            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// handler for unchecking check box to choose to not query by type
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueryReportsByType_UnChecked(object sender, RoutedEventArgs e)
        {
            queryByType = false;
            isQueryAllNew = true;
            isQueryAllByTypeNew = true;
            isQueryMyNew = true;
            isQueryMyByTypeNew = true;
            ReportTypeCombobox.Visibility = Visibility.Collapsed;
            SortByTypeCheckBox.IsEnabled = true;
            SortByTypeSelectGroupTextblock.Foreground = new SolidColorBrush(Colors.Black);
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// clears map
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearMapNow_Click(object sender, RoutedEventArgs e)
        {
            ClearMap();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarkBuildings_Click(object sender, RoutedEventArgs e)
        {
            Buildings buildings = new Buildings();
            if (!buildingsAdded)
            {
                MarkBuildings.Visibility = Visibility.Collapsed;
                try
                {
                    buildingsAdded = true;
                    foreach (Building building in buildings.GetList())
                    {
                        AddToMap(building);
                    }
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// handler for checking check box to choose to sort query
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortReports_Checked(object sender, RoutedEventArgs e)
        {
            sortSelectCount = 0;
            SortGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// handler for unchecking check box to choose to not sort
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortReports_UnChecked(object sender, RoutedEventArgs e)
        {
            SortGrid.Visibility = Visibility.Collapsed;
            sortSelectCount = 0;
            for (int i = 0; i <= 5; i++)
            {
                sortSelect[i] = sortSelectCount;
            }

            ByTypeSelectedTextblock.Text = sortSelect[0].ToString();
            ByDateSelectedTextblock.Text = sortSelect[1].ToString();
            ByAuthorSelectedTextblock.Text = sortSelect[2].ToString();
            ByStatusSelectedTextblock.Text = sortSelect[3].ToString();
            ByPrioritySelectedTextblock.Text = sortSelect[4].ToString();
            ByFrequencySelectedTextblock.Text = sortSelect[5].ToString();

            SortByTypeCheckBox.IsChecked = false;
            SortByStatusCheckBox.IsChecked = false;
            SortByPriorityCheckBox.IsChecked = false;
            SortByDateCheckBox.IsChecked = false;
            SortByAuthorCheckBox.IsChecked = false;
            SortByFrequencyCheckBox.IsChecked = false;

            isQueryAllNew = true;
            isQueryAllByTypeNew = true;
            isQueryMyNew = true;
            isQueryMyByTypeNew = true;
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByType_Checked(object sender, RoutedEventArgs e)
        {
            sortSelectCount++;
            sortSelect[0] = sortSelectCount;
            ByTypeSelectedTextblock.Text = sortSelect[0].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByType_UnChecked(object sender, RoutedEventArgs e)
        {
            int temp = sortSelect[0];
            sortSelectCount--;
            sortSelect[0] = 0;
            ByTypeSelectedTextblock.Text = "0";
            for (int i = 0; i <= 5; i++)
            {
                if (temp < sortSelect[i] && sortSelect[i] > 0)
                {
                    UpdateSortOrderTextBox(i);
                    sortSelect[i]--;
                }
            }

            ByTypeSelectedTextblock.Text = sortSelect[0].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByDate_Checked(object sender, RoutedEventArgs e)
        {
            sortSelectCount++;
            sortSelect[1] = sortSelectCount;
            ByDateSelectedTextblock.Text = sortSelect[1].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByDate_UnChecked(object sender, RoutedEventArgs e)
        {
            int temp = sortSelect[1];
            sortSelectCount--;
            sortSelect[1] = 0;
            ByDateSelectedTextblock.Text = "0";
            for (int i = 0; i <= 5; i++)
            {
                if (temp < sortSelect[i] && sortSelect[i] > 0)
                {
                    UpdateSortOrderTextBox(i);
                    sortSelect[i]--;
                }
            }
            ByDateSelectedTextblock.Text = sortSelect[1].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByAuthor_Checked(object sender, RoutedEventArgs e)
        {
            sortSelectCount++;
            sortSelect[2] = sortSelectCount;
            ByAuthorSelectedTextblock.Text = sortSelect[2].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByAuthor_UnChecked(object sender, RoutedEventArgs e)
        {
            int temp = sortSelect[2];
            sortSelectCount--;
            sortSelect[2] = 0;
            ByAuthorSelectedTextblock.Text = "0";
            for (int i = 0; i <= 5; i++)
            {
                if (temp < sortSelect[i] && sortSelect[i] > 0)
                {
                    UpdateSortOrderTextBox(i);
                    sortSelect[i]--;
                }
            }
            ByAuthorSelectedTextblock.Text = sortSelect[2].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByStatus_Checked(object sender, RoutedEventArgs e)
        {
            sortSelectCount++;
            sortSelect[3] = sortSelectCount;
            ByStatusSelectedTextblock.Text = sortSelect[3].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByStatus_UnChecked(object sender, RoutedEventArgs e)
        {
            int temp = sortSelect[3];
            sortSelectCount--;
            sortSelect[3] = 0;
            ByStatusSelectedTextblock.Text = "0";
            for (int i = 0; i <= 5; i++)
            {
                if (temp < sortSelect[i] && sortSelect[i] > 0)
                {
                    UpdateSortOrderTextBox(i);
                    sortSelect[i]--;
                }
            }
            ByStatusSelectedTextblock.Text = sortSelect[3].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByPriority_Checked(object sender, RoutedEventArgs e)
        {
            sortSelectCount++;
            sortSelect[4] = sortSelectCount;
            ByPrioritySelectedTextblock.Text = sortSelect[4].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByPriority_UnChecked(object sender, RoutedEventArgs e)
        {
            int temp = sortSelect[4];
            sortSelectCount--;
            sortSelect[4] = 0;
            ByPrioritySelectedTextblock.Text = "0";
            for (int i = 0; i <= 5; i++)
            {
                if (temp < sortSelect[i] && sortSelect[i] > 0)
                {
                    UpdateSortOrderTextBox(i);
                    sortSelect[i]--;
                }
            }
            ByPrioritySelectedTextblock.Text = sortSelect[4].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByFrequency_Checked(object sender, RoutedEventArgs e)
        {
            sortSelectCount++;
            sortSelect[5] = sortSelectCount;
            ByFrequencySelectedTextblock.Text = sortSelect[5].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortByFrequency_UnChecked(object sender, RoutedEventArgs e)
        {
            int temp = sortSelect[5];
            sortSelectCount--;
            sortSelect[5] = 0;
            ByFrequencySelectedTextblock.Text = "0";
            for (int i = 0; i <= 5; i++)
            {
                if (temp < sortSelect[i] && sortSelect[i] > 0)
                {
                    UpdateSortOrderTextBox(i);
                    sortSelect[i]--;
                }
            }
            ByFrequencySelectedTextblock.Text = sortSelect[5].ToString();
            ResetQuery();
            if (inSession)
            {
                ShowReports();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshMap_Checked(object sender, RoutedEventArgs e)
        {
            refreshMap = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshMap_UnChecked(object sender, RoutedEventArgs e)
        {
            refreshMap = false;
        }

        /// <summary>
        /// resetform button handler
        /// prompts user to confirm or cancel
        /// 
        /// confirm: invokes resetreportform
        /// cancel: return
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ClearForm_Clicked(object sender, RoutedEventArgs e)
        {
            // Create the message dialog and set its content and title 
            var messageDialog = new MessageDialog("You have requested to reset the request form. This will remove description, current photo, and current video.", "Reset request form");

            // Add commands and set their callbacks 
            messageDialog.Commands.Add(new UICommand("Reset Request Form", (command) =>
            {
                //NotifyUser("Request form reset", NotifyType.StatusMessage);
                ResetReportForm();
            }));

            messageDialog.Commands.Add(new UICommand("Cancel", (command) =>
            {
                //NotifyUser("Reset request form canceled", NotifyType.StatusMessage);
            }));

            // Set the command that will be invoked by default 
            messageDialog.DefaultCommandIndex = 1;

            // Show the message dialog 
            await messageDialog.ShowAsync();
        }

        /// <summary>
        /// combobox handler to set report type
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            reportType = TypeFromIndex(((ComboBox)sender).SelectedIndex);
        }

        /// <summary>
        /// combobox handler to set query report type
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            queryReportType = TypeFromIndex(((ComboBox)sender).SelectedIndex).Trim();
            isQueryAllByTypeNew = true;
            isQueryMyByTypeNew = true;
            isQueryPaged = false;
            if (inSession)
            {
                ShowReports();
                isQueryPaged = true;
            }
        }

        /// <summary>
        /// Handles the Click event on the Button inside the Popup control and 
        /// closes the Popup. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseReportPopup_Clicked(object sender, RoutedEventArgs e)
        {
            // if the Popup is open, then close it 
            if (ReportPopup.IsOpen) { ReportPopup.IsOpen = false; }
            if (reportsChanged)
            {
                reportsChanged = false;
                isQueryAllNew = true;
                isQueryMyNew = true;
                isQueryAllByTypeNew = true;
                isQueryMyByTypeNew = true;
                ShowReports();
            }
            NotifyUser("Welcome to SIU 311", NotifyType.DisplayMessage);
        }       

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelReport_Clicked(object sender, RoutedEventArgs e)
        {
            // if the Popup is open, then close it 
            if (ReportFormPopup.IsOpen) { ReportFormPopup.IsOpen = false; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CloseReportClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                bool confirm_modify = await ConfirmOpenCloseReport(selectedNumber, true);
                if (confirm_modify)
                {
                    reportsChanged = await SIU311Service.CloseReportAsync(selectedNumber);
                    await ShowReportPopup(selectedNumber.ToString(), false);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OpenReportClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                bool confirm_modify = await ConfirmOpenCloseReport(selectedNumber, false);
                if (confirm_modify)
                {
                    reportsChanged = await SIU311Service.OpenReportAsync(selectedNumber);
                    await ShowReportPopup(selectedNumber.ToString(), false);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ElevateReportClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                bool confirm_modify = await ConfirmElevateReport(selectedNumber);
                if (confirm_modify)
                {
                    await SIU311Service.ElevateReportAsync(selectedNumber);
                    //reportsChanged
                    await ShowReportPopup(selectedNumber.ToString(), false);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ClaimReportClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                bool confirm_modify = await ConfirmClaimReport(selectedNumber);
                if (confirm_modify)
                {
                    await SIU311Service.ClaimReportAsync(selectedNumber, UserNameBlock.Text);
                    isQueryAllNew = true;
                    isQueryAllByTypeNew = true;
                    ShowReports();
                    if (ReportPopup.IsOpen) { ReportPopup.IsOpen = false; }
                    //await ShowReportPopup(selectedNumber.ToString(), false);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RemoveReportClicked(object sender, RoutedEventArgs e)
        {
            bool remove_report = await ConfirmRemoveMessage(selectedNumber, false);
            if (remove_report == true)
            {
                remove_report = false;
                if (await SIU311Service.RemoveReportAsync(UserNameBlock.Text, selectedNumber))
                {
                    PromptMessage("Report " + selectedNumber + " has been removed");
                    ResetSession();
                }
                else
                {
                    PromptMessage("You can not remove report " + selectedNumber + ". This report does not belong to you.");
                }
                if (ReportPopup.IsOpen) { ReportPopup.IsOpen = false; }
                selectedNumber = 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RemoveQueuedReportClicked(object sender, RoutedEventArgs e)
        {
            bool remove_report = await ConfirmRemoveMessage(selectedNumber, true);
            if (remove_report == true)
            {
                if (ReportPopup.IsOpen) { ReportPopup.IsOpen = false; }
                remove_report = false;
                if (await RemoveQueuedReport(selectedNumber))
                {
                    PromptMessage("Report " + selectedNumber + " has been removed");
                    RefreshQueue();
                }
                else
                {
                    PromptMessage("Could not remove report " + selectedNumber);
                }
                selectedNumber = 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SubmitQueuedReportClicked(object sender, RoutedEventArgs e)
        {
            ReportPopupProgressBar.Visibility = Visibility.Visible;
            await SubmitQueuedReport(queuedObjectList.ElementAt(-selectedNumber - 1));
            if (ReportPopup.IsOpen) { ReportPopup.IsOpen = false; }
            ReportPopupProgressBar.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            StopTracking();
            if (haveInternetAccess)
            {
                if (inSession)
                {
                    await EndSession();
                }
            }
            App.Current.Exit();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (haveInternetAccess = CheckForInternet())
            {
                //NotifyUser("Internet.", NotifyType.ReportMessage);
                SIU311Service = new SIUC311ServiceRef.I311ServiceClient();

                if (inSession = await BeginSession())
                {
                    ConnectButton.Visibility = Visibility.Collapsed;
                    ShowReports();
                }
                if (Convert.ToBoolean(SIUC311.SettingsView.localSettings.Values["AutoSubmit"]))
                {
                    if (queuedReportCount > 0)
                    {
                        await SubmitQueue();
                    }
                }
            }
            else
            {
                await WaitablePromptMessage("No internet connection found.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            PromptMessage("To submit a report:\n\n  1) click Take Photo on top right of screen\n  2) select a report type on the Form\n  3) enter a description\n  4) enter a location\n  5) click Submit\n\n");
        }
        #endregion

        #region Unused Methods
        /*
        /// <summary>
        /// 
        /// </summary>
        /// <param name="zippedData"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] zippedData)
        {
            byte[] decompressedData = null;
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (MemoryStream inputStream = new MemoryStream(zippedData))
                {
                    using (GZipStream zip = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        zip.CopyTo(outputStream);
                    }
                }
                decompressedData = outputStream.ToArray();
            }

            return decompressedData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainData"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] plainData)
        {
            byte[] compressesData = null;
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    zip.Write(plainData, 0, plainData.Length);
                }
                //Dont get the MemoryStream data before the GZipStream is closed 
                //since it doesn’t yet contain complete compressed data.
                //GZipStream writes additional data including footer information when its been disposed
                compressesData = outputStream.ToArray();
            }

            return compressesData;
        }
        */
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    class MemoryRandomAccessStream : IRandomAccessStream
    {
        private Stream m_InternalStream;
        public MemoryRandomAccessStream(Stream stream)
        {
            this.m_InternalStream = stream;
        }
        public MemoryRandomAccessStream(byte[] bytes)
        {
            this.m_InternalStream = new MemoryStream(bytes);
        }
        public IInputStream GetInputStreamAt(ulong position)
        {
            this.m_InternalStream.Seek((long)position, SeekOrigin.Begin);
            return this.m_InternalStream.AsInputStream();
        }
        public IOutputStream GetOutputStreamAt(ulong position)
        {
            this.m_InternalStream.Seek((long)position, SeekOrigin.Begin);
            return this.m_InternalStream.AsOutputStream();
        }
        public ulong Size
        {
            get { return (ulong)this.m_InternalStream.Length; }
            set { this.m_InternalStream.SetLength((long)value); }
        }
        public bool CanRead
        {
            get { return true; }
        }
        public bool CanWrite
        {
            get { return true; }
        }
        public IRandomAccessStream CloneStream()
        {
            throw new NotSupportedException();
        }
        public ulong Position
        {
            get { return (ulong)this.m_InternalStream.Position; }
        }
        public void Seek(ulong position)
        {
            this.m_InternalStream.Seek((long)position, 0);
        }
        public void Dispose()
        {
            this.m_InternalStream.Dispose();
        }
        public Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            var inputStream = this.GetInputStreamAt(0);
            return inputStream.ReadAsync(buffer, count, options);
        }
        public Windows.Foundation.IAsyncOperation<bool> FlushAsync()
        {
            var outputStream = this.GetOutputStreamAt(0);
            return outputStream.FlushAsync();
        }
        public Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            var outputStream = this.GetOutputStreamAt(0); return outputStream.WriteAsync(buffer);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ReportObject
    {                                       // Microsoft SQL dbo.PhotoReports
        private int report_id;              // rid   int primary key
        private string report_type;         // rtype varchar(50)
        private string report_author;       // rown  varchar(50)
        private string report_description;  // rdesc varchar(500)
        private string report_location;     // rdesc varchar(250)
        private DateTime report_time;       // rtime datetime
        private string report_latitude;     // rlat  varchar(20)        
        private string report_longitude;    // rlon  varchar(20)
        private string report_accuracy;     // racc  varchar(20)
        private string report_direction;    // rdir  varchar(20)

        public ReportObject(int id,
                            string type,
                            string author,
                            string description,
                            string location,
                            DateTime time,
                            string latitude,
                            string longitude,
                            string accuracy,
                            string direction)
        {
            report_id = id;
            report_type = type;
            report_author = author;
            report_description = description;
            report_location = location;
            report_time = time;
            report_latitude = latitude;
            report_longitude = longitude;
            report_accuracy = accuracy;
            report_direction = direction;
        }

        public int ReportId
        {
            get { return report_id; }
            set { report_id = value; }
        }

        public string ReportAuthor
        {
            get { return report_author; }
            set { report_author = value; }
        }

        public string ReportType
        {
            get { return report_type; }
            set { report_type = value; }
        }

        public string ReportDescription
        {
            get { return report_description; }
            set { report_description = value; }
        }

        public string ReportLocation
        {
            get { return report_location; }
            set { report_location = value; }
        }

        public DateTime ReportTime
        {
            get { return report_time; }
            set { report_time = value; }
        }

        public string ReportLatitude
        {
            get { return report_latitude; }
            set { report_latitude = value; }
        }

        public string ReportLongitude
        {
            get { return report_longitude; }
            set { report_longitude = value; }
        }

        public string ReportAccuracy
        {
            get { return report_accuracy; }
            set { report_accuracy = value; }
        }

        public string ReportDirection
        {
            get { return report_direction; }
            set { report_direction = value; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MainPageSizeChangedEventArgs : EventArgs
    {
        private ApplicationViewState viewState;

        public ApplicationViewState ViewState
        {
            get { return viewState; }
            set { viewState = value; }
        }
    }

    /// <summary>
    ///
    /// </summary>
    public enum NotifyType
    {
        StatusMessage,
        DisplayMessage,
        ReportMessage,
        QueueMessage,
        ErrorMessage
    };
}