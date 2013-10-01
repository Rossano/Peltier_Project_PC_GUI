using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Peltier_GUI
{
    public partial class MainWindow
    {
        private COM_Window comWindow;

        private void COM_Option_Executed(object sender, RoutedEventArgs e)
        {
            comWindow = new COM_Window(this);            
            comWindow.ShowDialog();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GraphOptions_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PID_Options_Click(object sender, RoutedEventArgs e)
        {

        }

        private void HelpMenu_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Inscrisci qui Help Menu", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutItem_Click(object sender, RoutedEventArgs e)
        {
            string msg = "Graphical User Interface per cella di Peltier\n\ncopyrigth A.R.A.\nVersione: " +
                string.Format("{0}\n", revisionString); ;
            System.Windows.MessageBox.Show(msg, "Peltier GUI", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
