using System.Windows;
using System.Windows.Media;

namespace HandleManager {
    /// <summary>
    /// Lógica de interacción para ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : Window {
        private Point point;
        private int pixel;

        public ColorPicker() {
            InitializeComponent();
        }

        public Point Location {
            get { return point; }
            set {
                point = value;
                left.Text = point.X.ToString();
                top.Text = point.Y.ToString();
            }
        }

        public int Zoom {
            get { return pixel; }
            set {
                pixel = value;
                zooming.Text = value.ToString();
                
                obj.StrokeThickness = pixel;
                var l = 44 - (pixel / 2);
                var i = 44 + (pixel / 2);
                obj.Data = PathGeometry.Parse("M0,44h" + l + " M" + i + ",44h" + l + " M44,0v" + l + " M44," + i + "v" + l);
            }
        }

        public Color Color {
            get { return ((SolidColorBrush)scColor.Background).Color; }
            set { scColor.Background = new SolidColorBrush(value); }
        }

        public ImageSource Source {
            get { return imgz.ImageSource; }
            set { imgz.ImageSource = value; }
        }
    }
}
