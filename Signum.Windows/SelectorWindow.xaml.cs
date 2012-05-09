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
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using Signum.Utilities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for SelectorWindow.xaml
    /// </summary>
    public partial class SelectorWindow : Window
    {
        public class ElementInfo
        {
            public object Element { get; set; }
            public ImageSource Image { get; set; }
            public string Text { get; set; }
        }

        public static readonly DependencyProperty TypesProperty =
            DependencyProperty.Register("Elements", typeof(ElementInfo[]), typeof(SelectorWindow), new UIPropertyMetadata(null));
        public ElementInfo[] Elements
        {
            get { return (ElementInfo[])GetValue(TypesProperty); }
            set { SetValue(TypesProperty, value); }
        }

        public static readonly DependencyProperty SelectedElementProperty =
            DependencyProperty.Register("SelectedElement", typeof(object), typeof(SelectorWindow), new UIPropertyMetadata(null));
        public object SelectedElement
        {
            get { return (object)GetValue(SelectedElementProperty); }
            set { SetValue(SelectedElementProperty, value); }
        }


        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(SelectorWindow), new UIPropertyMetadata(null));
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }


        public static bool ShowDialog<T>(IEnumerable<T> elements, out T selectedElement,
            Func<T, ImageSource> elementIcon = null,
            Func<T, string> elementText = null,
            string title = null,
            string message = null,
            Window owner = null)
        {
            if (title == null)
                title = Signum.Windows.Properties.Resources.SelectAnElement;

            if (message == null)
                message = Signum.Windows.Properties.Resources.SelectAnElement;

            if (elementIcon == null)
                elementIcon = o => null;

            if (elementText == null)
                elementText = o => o.ToString();

            SelectorWindow w = new SelectorWindow()
            {
                Owner = owner,
                Title = title,
                Message = message,
                Elements = elements.Select(e => new ElementInfo()
                {
                    Element = e,
                    Image = elementIcon(e),
                    Text = elementText(e)
                }).ToArray()
            };
            bool res = w.ShowDialog() ?? false;
            if (res)
                selectedElement = (T)w.SelectedElement;
            else
                selectedElement = default(T);
            return res;
        }

        public SelectorWindow()
        {
            InitializeComponent();

            this.Message = Signum.Windows.Properties.Resources.SelectAnElement;
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            SelectedElement = ((ElementInfo)((ToggleButton)sender).DataContext).Element;
            DialogResult = true;
            Close();
        }
    }
}
