using MultipleMusicPlayer.Music;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultipleMusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MusicList musicList = new MusicList();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            musicList.AddDirectory("E:/kugou/");
            musicListPanel.DataContext = musicList;
            Debug.WriteLine("123456");
        }
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (sender is ListBoxItem item) {
                if (item.Content is IMusic musicFile) {
                    musicFile.Play();
                    //Debug.WriteLine(musicFile.Name);
                }
                //Debug.WriteLine(item.Content.GetType());
            }
            //Debug.WriteLine(sender.GetType());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            musicList.Stop();
        }
    }
}
