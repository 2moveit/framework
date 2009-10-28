﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Windows.Widgets;
using Signum.Services;

namespace Signum.Windows.Basics
{
    /// <summary>
    /// Interaction logic for Alert.xaml
    /// </summary>
    public partial class Alert : UserControl
    {
        public Alert()
        {
            InitializeComponent();
        }

        public static void Start()
        {
            WidgetPanel.GetWidgets += (obj, mainControl) => obj is IdentifiableEntity && !(obj is IAlertDN || ((IdentifiableEntity)obj).IsNew) ? new AlertsWidget() : null;

            AlertsWidget.CreateAlert = ei => ei.IsNew ? null : new Signum.Entities.Basics.AlertDN { Entity = ei.ToLite() };
            AlertsWidget.RetrieveAlerts = ei => ei == null ? null : Server.Service<IAlertsServer>().RetrieveAlerts(ei.ToLite());

            Navigator.Manager.Settings.Add(typeof(Alert), new EntitySettings { View = () => new Alert(), IsCreable = admin => false, Icon = BitmapFrame.Create(PackUriHelper.Reference("/Images/alert.png", typeof(AlertsWidget))) });
        }
    }
}
