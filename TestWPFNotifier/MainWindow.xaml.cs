//
//      MainWindow.xaml.cs
//
//      Operation of the test application
//
using System;
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

using WPFNotify;                                            // Add a reference to your project and then include this NS

namespace TestWPFNotifier
{
    /// <summary>
    /// Frontend operations - MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Main constructor
        //-------------------------------------------------------------------------------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Event for "Notify" button
        //-------------------------------------------------------------------------------------------------------------------------------
        private void onNotifyButtonClick(object sender, RoutedEventArgs e)
        {
            Notifier.Type noteType = Notifier.Type.INFO;

            if ((bool) radioButtonOk.IsChecked)
                noteType = Notifier.Type.OK;                        // Note type INFO

            if ((bool)radioButtonWarning.IsChecked)
                noteType = Notifier.Type.WARNING;                   // Note type OK

            if ((bool)radioButtonError.IsChecked)
                noteType = Notifier.Type.ERROR;                     // Note type WARNING

            int time_out = 0;
            Int32.TryParse(timeout.Text, out time_out);             // Note type ERROR

            if (!(bool)inApp.IsChecked)
            {
                short ID = Notifier.Show(textNote.Text,             // It is possible to get the ID of the note on the Creation
                                         noteType, 
                                         textTitle.Text, 
                                         false, 
                                         time_out);
            }
            else
            {
                short ID = Notifier.Show(textNote.Text,             // It is possible to get the ID of the note on the Creation
                                         noteType, 
                                         textTitle.Text, 
                                         false, 
                                         time_out, 
                                         this);
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Event for "Notify Dialog" button
        //-------------------------------------------------------------------------------------------------------------------------------
        private void oNotifyDialogButtonClick(object sender, RoutedEventArgs e)
        {
            Notifier.Type noteType = Notifier.Type.INFO;
            BackDialogStyle back = BackDialogStyle.FadedApplication;

            if ((bool)radioButtonInfo.IsChecked)
                noteType = Notifier.Type.INFO;                      // Note type INFO

            if ((bool)radioButtonOk.IsChecked)
                noteType = Notifier.Type.OK;                        // Note type OK

            if ((bool)radioButtonWarning.IsChecked)
                noteType = Notifier.Type.WARNING;                   // Note type WARNING

            if ((bool)radioButtonError.IsChecked)
                noteType = Notifier.Type.ERROR;                     // Note type ERROR

            if ((bool)backNone.IsChecked)
                back = BackDialogStyle.None;                        // Faded Background type: None

            if ((bool)backApp.IsChecked)
                back = BackDialogStyle.FadedApplication;            // Faded Background type: Application

            if ((bool)backFull.IsChecked)
                back = BackDialogStyle.FadedScreen;                 // Faded Background type: Fullscreen

            Notifier.ShowDialog(textNote.Text,                      // It is not possible to get the ID of the note on the Creation for dialogs
                                noteType, 
                                textTitle.Text, 
                                back, 
                                this);
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Event for "Update" button
        //-------------------------------------------------------------------------------------------------------------------------------
        private void onUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            Notifier.Type noteType = Notifier.Type.INFO;

            if ((bool)radioButtonInfo.IsChecked)
                noteType = Notifier.Type.INFO;

            if ((bool)radioButtonOk.IsChecked)
                noteType = Notifier.Type.OK;

            if ((bool)radioButtonWarning.IsChecked)
                noteType = Notifier.Type.WARNING;

            if ((bool)radioButtonError.IsChecked)
                noteType = Notifier.Type.ERROR;

            int updateIdNote = 0;
            Int32.TryParse(numericNote.Text, out updateIdNote);

            Notifier.Update((short) updateIdNote,                       // Update the note
                            textNote.Text, 
                            noteType, 
                            textTitle.Text);
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Event for window Close
        //-------------------------------------------------------------------------------------------------------------------------------
        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Notifier.CloseAll();                                        // Close all the notes
        }
    }
}
