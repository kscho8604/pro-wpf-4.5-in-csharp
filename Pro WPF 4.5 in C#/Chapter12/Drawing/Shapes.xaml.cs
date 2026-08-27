using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Drawing
{
    /// <summary>
    /// Interaction logic for Shapes.xaml
    /// </summary>

    public partial class Shapes : System.Windows.Window
    {

        public Shapes()
        {
            InitializeComponent();

            foreach (var child in MyStackPanel.Children)
            {
                if (child is Ellipse ellipse)
                {
                    ellipse.Fill = Brushes.Green; // 원하는 변경 작업
                    ellipse.Stroke = Brushes.Black;
                    ellipse.Width = 150;
                    ellipse.Height = 75;

                    break;
                }
            }
        }

        private void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Shape clickedShape)
            {
                clickedShape.Fill = Brushes.Red;
                clickedShape.Stroke = Brushes.Black;
                clickedShape.Width = 75;
                clickedShape.Height = 75;
            }
        }

    }
}