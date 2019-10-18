﻿using MultipleMusicPlayer.Music;
using System;
using System.Collections.Generic;
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
        }
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (sender is ListBoxItem item) {
                if (item.Content is IMusic musicFile) {
                    musicFile.Play();
                    //Console.WriteLine(musicFile.Name);
                }
                //Console.WriteLine(item.Content.GetType());
            }
            //Console.WriteLine(sender.GetType());
        }
    }
}
