﻿// file:	CrittercismSDK\Crittercism.cs
// summary:	Implements the crittercism class
namespace CrittercismSDK {
    using CrittercismSDK.DataContracts;
    using CrittercismSDK.DataContracts.Legacy;
    using Microsoft.Phone.Shell;
    using Microsoft.Phone.Net.NetworkInformation;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Windows;

    /// <summary>
    /// Crittercism.
    /// </summary>
    public class Crittercism {
        #region Properties
        /// <summary>
        /// The auto run queue reader
        /// </summary>
        internal static bool _autoRunQueueReader = true;

        /// <summary>
        /// The enable communication layer
        /// </summary>
        internal static bool _enableCommunicationLayer = true;

        /// <summary>
        /// The enable raise exception in communication layer
        /// </summary>
        internal static bool _enableRaiseExceptionInCommunicationLayer = false;

        /// <summary>
        /// Gets or sets a queue of messages.
        /// </summary>
        /// <value> A Queue of messages. </value>
        internal static Queue<CrittercismSDK.DataContracts.MessageReport> MessageQueue { get; set; }

        /// <summary>
        /// Gets or sets the current breadcrumbs.
        /// </summary>
        /// <value> The breadcrumbs. </value>
        internal static CrittercismSDK.DataContracts.Legacy.Breadcrumbs CurrentBreadcrumbs { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the application.
        /// </summary>
        /// <value> The identifier of the application. </value>
        internal static string AppID { get; set; }


        internal static bool OptOut {get; set; }

        /// <summary>
        /// Gets or sets the operating system platform.
        /// </summary>
        /// <value> The operating system platform. </value>
        internal static string OSPlatform { get; set; }

        /// <summary>
        /// Gets or sets the arbitrary user metadata.
        /// </summary>
        /// <value> The user metadata. </value>
        internal static Dictionary<string, string> ArbitraryUserMetadata { get; set; }

        /// <summary>
        /// Folder name for the messages files
        /// </summary>
        internal static string FolderName = "CrittercismMessages";

        /// <summary> 
        /// Message Counter
        /// </summary>
        internal static int messageCounter = 0;

        /// <summary> 
        /// The initial date
        /// </summary>
        internal static DateTime initialDate = DateTime.Now;

        /// <summary>
        /// The thread for the reader
        /// </summary>
        internal static Thread readerThread = null;

        #endregion

        #region Methods

        internal static readonly string CrittercismUserMetadataKey = "crittercism_user_metadata";
        static Crittercism() {
            ArbitraryUserMetadata = LoadUserMetadataFromDisk();
        }

        internal static Dictionary<string,string> LoadUserMetadataFromDisk() {
            try {
                if (System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings.
                    Contains(CrittercismUserMetadataKey)) {
                    return new Dictionary<string, string>((Dictionary<string, string>)
                        System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings
                            [CrittercismUserMetadataKey]);
                }
            } catch {
                // nop
            }
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Initialises this object.
        /// </summary>
        /// <param name="appID">  Identifier for the application. </param>
        public static void Init(string appID) {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex("^[0-9a-fA-F]{24}$");
            if(!r.IsMatch(appID)) {
                throw new InvalidAppIdException("Invalid AppId in Init. AppId should be 24 hex characters from the Crittercism portal.");
            }

            OptOut = CheckOptOutFromDisk();
            QueueReader queueReader = new QueueReader();
            ThreadStart threadStart = new ThreadStart(queueReader.ReadQueue);
            readerThread = new Thread(threadStart);
            readerThread.Name = "Crittercism Sender";
            StartApplication(appID);

            if (_autoRunQueueReader && _enableCommunicationLayer && !(_enableRaiseExceptionInCommunicationLayer))  // for unit test purposes
            {
                Application.Current.UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>(Current_UnhandledException);
                DeviceNetworkInformation.NetworkAvailabilityChanged += DeviceNetworkInformation_NetworkAvailabilityChanged;
                try {
                    if (PhoneApplicationService.Current != null) {
                        PhoneApplicationService.Current.Activated += new EventHandler<ActivatedEventArgs>(Current_Activated);
                        PhoneApplicationService.Current.Deactivated += new EventHandler<DeactivatedEventArgs>(Current_Deactivated);
                    }
                }
                catch {
                }
            }
            System.Diagnostics.Debug.WriteLine("Crittercism initialized.");
        }

        /// <summary>
        /// Sets a username.
        /// </summary>
        /// <param name="username"> The username. </param>
        public static void SetUsername(string username) {
            SetValue("username", username);
        }

        /// <summary>
        /// Sets an arbitrary user metadata value.
        /// </summary>
        /// <param name="key">      The key. </param>
        /// <param name="value">    The value. </param>
        public static void SetValue(string key, string value) {
            Dictionary<string, string> copyToSend = null; // set to non-null copy (for atomicity) if we should send metadata (because something changed)
            lock (ArbitraryUserMetadata) {
                if (!ArbitraryUserMetadata.ContainsKey(key) || !ArbitraryUserMetadata[key].Equals(value)) {
                    ArbitraryUserMetadata[key] = value;
                    copyToSend = new Dictionary<string, string>(ArbitraryUserMetadata);
                    System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings[CrittercismUserMetadataKey] = copyToSend;
                }
            } if (copyToSend != null) {
                string appVersion = System.Windows.Application.Current.GetType().Assembly.GetName().Version.ToString();
                CrittercismSDK.DataContracts.Legacy.UserMetadata um = new CrittercismSDK.DataContracts.Legacy.UserMetadata(
                    AppID, appVersion, new Dictionary<string, string>(ArbitraryUserMetadata));
                um.SaveToDisk();
                AddMessageToQueue(um);
            }
        }

        public static bool GetOptOutStatus() {
            return OptOut;
        }

        public static void SetOptOutStatus(bool optOut) {
            if (optOut == OptOut) {
                return; // mission accomplished
            }
            OptOut = optOut;
            SetOptOutOnDisk(optOut);
        }

        private static readonly string CrittercismOptOutKey = "crittercism_OptOut";
        private static void SetOptOutOnDisk(bool optOut) {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            if (optOut) {
                if (!System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings.Contains(CrittercismOptOutKey)) {
                    System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings.Add(CrittercismOptOutKey, "true");
                }
            }
            else {
                if (System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings.Contains(CrittercismOptOutKey)) {
                    System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings.Remove(CrittercismOptOutKey);
                }
            }
        }

        internal static bool CheckOptOutFromDisk() {
            try {
                return System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings.Contains(CrittercismOptOutKey);
            } catch {
                // swallow, best effort
                return false;
            }
        }

        /// <summary>
        /// Leave breadcrum.
        /// </summary>
        /// <param name="breadcrumb">   The breadcrumb. </param>
        public static void LeaveBreadcrumb(string breadcrumb) {
            lock (CurrentBreadcrumbs) {
                CurrentBreadcrumbs.current_session.Add(new BreadcrumbMessage(breadcrumb));
                CurrentBreadcrumbs.SaveToDisk();
            }
        }

        /// <summary>
        /// Creates the error report.
        /// </summary>
        public static void LogHandledException(Exception e) {
            if (OptOut) {
                return;
            }
            string appVersion = System.Windows.Application.Current.GetType().Assembly.GetName().Version.ToString();
            Breadcrumbs breadcrumbs = new Breadcrumbs();
            breadcrumbs.current_session = new List<BreadcrumbMessage>(CurrentBreadcrumbs.current_session);
            breadcrumbs.previous_session = new List<BreadcrumbMessage>(CurrentBreadcrumbs.previous_session);
            ExceptionObject exception = new ExceptionObject(e.GetType().FullName, e.Message, e.StackTrace);
            HandledException he = new HandledException(AppID, appVersion, new Dictionary<string, string>(ArbitraryUserMetadata), breadcrumbs, exception);
            he.SaveToDisk();
            AddMessageToQueue(he);
        }
        
        /// <summary>
        /// Creates a crash report.
        /// </summary>
        /// <param name="currentException"> The current exception. </param>
        internal static void CreateCrashReport(Exception currentException) {
            if (OptOut) {
                return;
            }
            string appVersion = System.Windows.Application.Current.GetType().Assembly.GetName().Version.ToString();
            Breadcrumbs breadcrumbs = new Breadcrumbs();
            breadcrumbs.current_session = new List<BreadcrumbMessage>(CurrentBreadcrumbs.current_session);
            breadcrumbs.previous_session = new List<BreadcrumbMessage>(CurrentBreadcrumbs.previous_session);
            ExceptionObject exception = new ExceptionObject(currentException.GetType().FullName, currentException.Message, currentException.StackTrace);
            Crash crash = new Crash(AppID, appVersion, new Dictionary<string,string>(ArbitraryUserMetadata), breadcrumbs, exception);
            crash.SaveToDisk();
            AddMessageToQueue(crash);
            CurrentBreadcrumbs.previous_session = new List<BreadcrumbMessage>(CurrentBreadcrumbs.current_session);
            CurrentBreadcrumbs.current_session.Clear();
        }

        /// <summary>
        /// Creates the application load report.
        /// </summary>
        private static void CreateAppLoadReport() {
            if (OptOut) {
                return;
            }

            CrittercismSDK.DataContracts.Unified.AppLoad appLoad = new CrittercismSDK.
                DataContracts.Unified.AppLoad(AppID);

            appLoad.SaveToDisk();
            AddMessageToQueue(appLoad);
        }

        /// <summary>
        /// Loads the messages from disk into the queue.
        /// </summary>
        private static void LoadQueueFromDisk()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            if (storage.DirectoryExists(FolderName))
            {
                string[] fileNames = storage.GetFileNames(FolderName + "\\*");
                List<MessageReport> messages = new List<MessageReport>();
                foreach (string file in fileNames)
                {
                    string[] fileSplit = file.Split('_');
                    MessageReport message = null;
                    switch (fileSplit[0])
                    {
                        // Note: this whole approach is wrong, we should be using immutable 
                        // data objects here, and maybe an interface rather an explicit object?
                        case "AppLoad":
                            message = new CrittercismSDK.DataContracts.Unified.AppLoad();
                            break;
                        case "HandledException":
                            message = new HandledException();
                            break;
                        case "Crash":
                            message = new Crash();
                            break;
                        case "UserMetadata":
                            message = new UserMetadata();
                            break;
                        default:
                            continue; // skip this file
                    }

                    message.Name = file;
                    message.CreationDate = storage.GetCreationTime(FolderName + "\\" + file);
                    message.IsLoaded = false;
                    messages.Add(message);
                }

                messages.Sort((m1, m2) => m1.CreationDate.CompareTo(m2.CreationDate));
                foreach (MessageReport message in messages)
                {
                    // I'm wondering if we needed to restrict to 50 message of something similar?
                    MessageQueue.Enqueue(message);
                }
            }
        }

        /// <summary>
        /// Adds  message to queue
        /// </summary>
        private static void AddMessageToQueue(CrittercismSDK.DataContracts.MessageReport message)
        {
            if (DateTime.Now.Subtract(initialDate) <= new TimeSpan(0, 0, 0, 1, 0))
            {
                messageCounter++;
            }
            else
            {
                messageCounter = 0;
                initialDate = DateTime.Now;
            }

            if (messageCounter < 50)
            {
                MessageQueue.Enqueue(message);
                if (_autoRunQueueReader)  // This flag is for unit test
                {
                    // FIXME jbley I don't like the threading here - would prefer one single background thread
                    // with blocking queue rather than this (duplicated-code) spin-a-thread-each-batch-of-messages
                    // stuff.
                    if (readerThread.ThreadState == ThreadState.Unstarted)
                    {
                        readerThread.Start();
                    }
                    else if (readerThread.ThreadState == ThreadState.Stopped || readerThread.ThreadState == ThreadState.Aborted)
                    {
                        QueueReader queueReader = new QueueReader();
                        ThreadStart threadStart = new ThreadStart(queueReader.ReadQueue);
                        readerThread = new Thread(threadStart);
                        readerThread.Name = "Crittercism Sender";
                        readerThread.Start();
                    }
                }
            }
            else
            {
                message.DeleteFromDisk();
            }
        }

        /// <summary>
        /// This method is invoked when the application starts or resume
        /// </summary>
        /// <param name="appID">    Identifier for the application. </param>
        private static void StartApplication(string appID)
        {
            AppID = appID;
            CurrentBreadcrumbs = Breadcrumbs.GetBreadcrumbs();
            OSPlatform = Environment.OSVersion.Platform.ToString();
            MessageQueue = new Queue<MessageReport>();
            LoadQueueFromDisk();
            CreateAppLoadReport();
        }

        /// <summary>
        /// This method is invoked when the application resume
        /// </summary>
        private static void StartApplication()
        {
            StartApplication(AppID);
        }

        /// <summary>
        /// Event handler. Called by Current for unhandled exception events.
        /// </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Application unhandled exception event information. </param>
        static void Current_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                CreateCrashReport((Exception)e.ExceptionObject);
            }
            catch
            {
                // explicit nop
            }
        }

        static void Current_Activated(object sender, ActivatedEventArgs e)
        {
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.RunWorkerAsync();
        }

        static void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            StartApplication((string)PhoneApplicationService.Current.State["Crittercism.AppID"]);
        }

        static void Current_Deactivated(object sender, DeactivatedEventArgs e)
        {
            PhoneApplicationService.Current.State["Crittercism.AppID"] = AppID;
        }

        static void DeviceNetworkInformation_NetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            if (_autoRunQueueReader)  // This flag is for unit test
            {
                switch (e.NotificationType)
                {
                    case NetworkNotificationType.InterfaceConnected:
                        if (NetworkInterface.GetIsNetworkAvailable())
                        {
                            if (MessageQueue != null && MessageQueue.Count > 0)
                            {
                                if (readerThread.ThreadState == ThreadState.Unstarted)
                                {
                                    readerThread.Start();
                                }
                                else if (readerThread.ThreadState == ThreadState.Stopped || readerThread.ThreadState == ThreadState.Aborted)
                                {
                                    QueueReader queueReader = new QueueReader();
                                    ThreadStart threadStart = new ThreadStart(queueReader.ReadQueue);
                                    readerThread = new Thread(threadStart);
                                    readerThread.Name = "Crittercism Sender";
                                    readerThread.Start();
                                }
                            }
                        }

                        break;
                }
            }
        }
        #endregion
    }
}