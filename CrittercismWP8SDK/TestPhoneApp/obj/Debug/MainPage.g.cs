﻿#pragma checksum "C:\Users\albre_000\code\windows-client\CrittercismWP8SDK\TestPhoneApp\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "DB91F49120E3582DEC31B085B672F6C4"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18010
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace TestPhoneApp {
    
    
    public partial class MainPage : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.StackPanel TitlePanel;
        
        internal System.Windows.Controls.Grid ContentPanel;
        
        internal System.Windows.Controls.ListBox UnitTestList;
        
        internal System.Windows.Controls.TextBlock TestGlobalStatusLabel;
        
        internal System.Windows.Controls.TextBlock TestPassLabel;
        
        internal System.Windows.Controls.TextBlock TestFailLabel;
        
        internal System.Windows.Controls.TextBlock TestTotalLabel;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/TestPhoneApp;component/MainPage.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.TitlePanel = ((System.Windows.Controls.StackPanel)(this.FindName("TitlePanel")));
            this.ContentPanel = ((System.Windows.Controls.Grid)(this.FindName("ContentPanel")));
            this.UnitTestList = ((System.Windows.Controls.ListBox)(this.FindName("UnitTestList")));
            this.TestGlobalStatusLabel = ((System.Windows.Controls.TextBlock)(this.FindName("TestGlobalStatusLabel")));
            this.TestPassLabel = ((System.Windows.Controls.TextBlock)(this.FindName("TestPassLabel")));
            this.TestFailLabel = ((System.Windows.Controls.TextBlock)(this.FindName("TestFailLabel")));
            this.TestTotalLabel = ((System.Windows.Controls.TextBlock)(this.FindName("TestTotalLabel")));
        }
    }
}

