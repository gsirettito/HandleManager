using MouseKeyboardLibrary;
using SiretT.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SDrawing = System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Collections;
using Microsoft.Build.Tasks;
using MS.Interop.WinUser;
using System.Text.RegularExpressions;

namespace HandleManager {
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private WindowInteropHelper wih;
        private AssemblyName assemblyName;
        private bool isSearching;
        private Window wm;
        private IntPtr wmHandle;
        private IntPtr last_hwnd;
        private bool isPickering;
        private ColorPicker cp;
        private int pixel = 8;
        private IntPtr cpHandle;
        private MouseHook mh;
        private SDrawing.Bitmap bmp;
        private System.Windows.Point last_p;

        public MainWindow() {
            InitializeComponent();
            wih = new WindowInteropHelper(this);
            assemblyName = Assembly.GetExecutingAssembly().GetName();
            Title = assemblyName.Name;
            mh = new MouseHook();
            mh.MouseWheel += Mh_MouseWheel;
            mh.MouseUp += Mh_MouseUp;
            mh.MouseDown += Mh_MouseUp;
            this.Closing += MainWindow_Closing;
        }

        public IntPtr Hwnd {
            get {
                try {
                    return new IntPtr(Convert.ToInt32(hwndText.Text, 16));
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message);
                    return IntPtr.Zero;
                }
            }
        }

        public bool IsIntPtr(string value, int fromBase = 10) {
            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                return false;
            try {
                new IntPtr(Convert.ToInt32(value, fromBase));
                return true;
            } catch { return false; }
        }

        public IntPtr Handle { get { return wih.Handle; } }

        public IntPtr DesktopHandle {
            get {
                return WinUser.GetDesktopWindow();
            }
        }

        public IntPtr SelectedHandle {
            get {
                return (new IntPtr(Convert.ToInt32(hwndText.Text, 16)));
            }
        }

        #region override

        /// <summary>
        /// AddHook Handle WndProc messages in WPF
        /// This cannot be done in a Window's constructor as a handle window handle won't at that point, so there won't be a HwndSource.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            HwndSource hwnd = PresentationSource.FromVisual(this) as HwndSource;
            hwnd.AddHook(WindowFilterMessage);
            FillTree(DesktopHandle, null);
            FillProcess();
        }

        private void FillProcess() {
            processlist.ItemsSource = Process.GetProcesses();
        }

        void FillTree(IntPtr hwnd, TreeViewItem root) {
            TreeViewItem tvi = new TreeViewItem() {
                Header = GetWindowToString(hwnd),
                HeaderTemplate = hwndtree.ItemTemplate,
                Tag = WinUser.IsWindowVisible(hwnd),
            };

            if (root == null) hwndtree.Items.Add(tvi);
            else root.Items.Add(tvi);
            List<IntPtr> childs = new List<IntPtr>();
            FindWindow(hwnd, IntPtr.Zero, childs);
            foreach (var i in childs) {
                FillTree(i, tvi);
            }
        }

        private void FindWindow(IntPtr parent, IntPtr prev, List<IntPtr> list) {
            var child = WinUser.FindWindowEx(parent, prev, null, null);
            if (child == IntPtr.Zero) return;
            list.Add(child);
            FindWindow(parent, child, list);
        }

        private IntPtr NextWindow(IntPtr owner) {
            return WinUser.FindWindowEx(WinUser.GetAncestor(owner, 1), owner, null, null);
        }

        private IntPtr PrevWindow(IntPtr owner) {
            var list = GetChildsWindows(WinUser.GetAncestor(owner, 1));
            int indx = list.IndexOf(owner);
            if (indx > 0)
                return list[indx - 1];
            return IntPtr.Zero;
        }

        private List<IntPtr> GetChildsWindows(IntPtr hwnd) {
            List<IntPtr> list = new List<IntPtr>();
            FindWindow(hwnd, IntPtr.Zero, list);
            return list;
        }

        #endregion


        #region messageFilter

        /// <summary>
        ///     This is the hook to HwndSource that is called when window messages related to
        ///     this window occur.
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WindowFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            IntPtr retInt = IntPtr.Zero;
            WindowMessage message = (WindowMessage)msg;

            //
            // we need to process WM_GETMINMAXINFO before _swh is assigned to
            // b/c we want to store the max/min size allowed by win32 for the hwnd
            // which is later used in GetWindowMinMax.  WmGetMinMaxInfo can handle
            // _swh == null case.
            switch (message) {
                case WindowMessage.WM_APP:
                    AddHandle(WinUser.GetForegroundWindow());
                    break;
            }

            return retInt;
        }

        #endregion

        #region Commands
        private void HighlightCommand(object sender, RoutedEventArgs e) {
            var item = ((FrameworkElement)sender).DataContext.ToString();
            var str_hwnd = Regex.Match(item, "[0-9A-F]{8}").Value;
            HighlightedWindow(new IntPtr(Convert.ToInt32(str_hwnd, 16)));
        }

        private void CopyHwndCommand(object sender, RoutedEventArgs e) {
            var item = ((FrameworkElement)sender).DataContext.ToString();
            if (Regex.IsMatch(item, "[0-9A-F]{8}")) {
                var str_hwnd = Regex.Match(item, "[0-9A-F]{8}").Value;
                System.Windows.Clipboard.SetText(str_hwnd);
            } else
                System.Windows.Clipboard.SetText("");
        }

        private void CopyTextCommand(object sender, RoutedEventArgs e) {
            var item = ((FrameworkElement)sender).DataContext.ToString();
            if (Regex.IsMatch(item, "\".+\"")) {
                var index = item.IndexOf("\"") + 1;
                var text = item.Substring(index, item.LastIndexOf("\"") - index);//Regex.Match(item, "\".+\"").Value;
                System.Windows.Clipboard.SetText(text.Substring(1, text.Length - 2));
            } else
                System.Windows.Clipboard.SetText("");
        }

        private void CopyClassNameCommand(object sender, RoutedEventArgs e) {
            var item = ((FrameworkElement)sender).DataContext.ToString();
            if (Regex.IsMatch(item, "\".+\" ")) {
                var rex = Regex.Match(item, "\".+\" ");
                var text = item.Substring(rex.Index + rex.Length);
                System.Windows.Clipboard.SetText(text);
            } else
                System.Windows.Clipboard.SetText("");
        }

        private void LocalizeInTreeCommand(object sender, RoutedEventArgs e) {
            var item = ((FrameworkElement)sender).DataContext.ToString();
            var str_hwnd = Regex.Match(item, "[0-9A-F]{8}").Value;
            FindInTree(str_hwnd);
        }

        private void NextWindowCommand(object sender, RoutedEventArgs e) {
            var item = ((FrameworkElement)sender).DataContext.ToString();
            var str_hwnd = Regex.Match(item, "[0-9A-F]{8}").Value;
            GoToWindow(str_hwnd, GoToNavigation.Next);
        }

        private void PrevWindowCommand(object sender, RoutedEventArgs e) {
            var item = ((FrameworkElement)sender).DataContext.ToString();
            var str_hwnd = Regex.Match(item, "[0-9A-F]{8}").Value;
            GoToWindow(str_hwnd, GoToNavigation.Previous);
        }

        private void NextWindowButtonClick(object sender, RoutedEventArgs e) {
            GoToWindow(Hwnd, GoToNavigation.Next);
        }

        private void PrevWindowButtonClick(object sender, RoutedEventArgs e) {
            GoToWindow(Hwnd, GoToNavigation.Previous);
        }

        private enum GoToNavigation {
            Next, Previous, Up, Ancestor = Up, Down, Child = Down
        }

        private void GoToWindow(string str_owner, GoToNavigation gotoNav) {
            GoToWindow(new IntPtr(Convert.ToInt32(str_owner, 16)), gotoNav);
        }

        private void GoToWindow(IntPtr owner, GoToNavigation gotoNav) {
            var hwnd = owner;
            switch (gotoNav) {
                case GoToNavigation.Next:
                    hwnd = NextWindow(owner);
                    break;
                case GoToNavigation.Previous:
                    hwnd = PrevWindow(owner);
                    break;
                case GoToNavigation.Ancestor:
                    hwnd = WinUser.GetAncestor(owner, 1);
                    break;
                case GoToNavigation.Child:
                    hwnd = WinUser.FindWindowEx(owner, IntPtr.Zero, null, null);
                    break;
            }
            if (hwnd != IntPtr.Zero)
                hwndText.Text = hwnd.ToString("X8");
            var item = GetWindowToString(hwnd);
            hwndlist.SelectedItem = item;
            hwndlist.BringIntoView();
        }

        private void FindInTree(string str_hwnd) {
            foreach (TreeViewItem i in hwndtree.Items) {
                TreeViewItem it = null;
                findInTree(i, ref it, str_hwnd);
                if (it != null) {
                    var orig = it;
                    it.IsSelected = true;
                    var parent = it.Parent as TreeViewItem;
                    while (parent != null) {
                        parent.IsExpanded = true;
                        parent = parent.Parent as TreeViewItem;
                        it = parent;
                    }
                    orig.BringIntoView();
                    return;
                }
            }
        }

        private void findInTree(TreeViewItem treeViewItem, ref TreeViewItem finded, string str) {
            foreach (TreeViewItem i in treeViewItem.Items) {
                if (i.Header.ToString().Contains(str)) {
                    finded = i;
                    return;
                } else findInTree(i, ref finded, str);
            }
        }
        #endregion

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            VisualChildren(this);
        }

        private void VisualChildren(DependencyObject element) {
            int count = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count; i++) {
                var child = VisualTreeHelper.GetChild(element, i);
                if (child is Window || child.ToString() == "Microsoft.VisualStudio.DesignTools.WpfDesigner.InstanceBuilders.WindowInstance")
                    (child as Window).Close();
                if (child is TabWindow) {
                    foreach (var j in (child as TabWindow).Items) {
                        if (j is Window)
                            (j as Window).Close();
                    }
                }
                VisualChildren(child);
            }
        }

        private void Mh_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
            if (cp != null) picker_MouseUp(sender, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            if (wm != null) search_MouseUp(sender, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            //if (isSearching) {
            //    isSearching = false;
            //    wm.Close();
            //    if (cp != null)
            //        cp.Close();
            //    mh.Stop();
            //    this.Cursor = Cursors.Arrow;
            //}
            //var p = GlobalMouse.Position;
            //IntPtr _hwnd = WinUser.WindowFromPoint(new MS.Interop.WinUser.POINT() { x = (int)p.X, y = (int)p.Y });
            //AddHandle(_hwnd);
        }

        private void Mh_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e) {
            if (isPickering && cp != null) {
                if (e.Delta > 0 && pixel < 15)
                    pixel += 2;
                else if (pixel > 2) pixel -= 2;
                cp.Zoom = pixel;
                int w = ((88 - pixel) / 2 / pixel * pixel * 2) + pixel;
                int h = ((88 - pixel) / 2 / pixel * pixel * 2) + pixel;
                ChangePosition(50, 50, w, h, pixel);
            }
        }

        private void ShowHideClick(object sender, RoutedEventArgs e) {
            WinUser.ShowWindow(Hwnd, WinUser.IsWindowVisible(Hwnd) == true ? 0 : 1);
            s_hButton.IsChecked = WinUser.IsWindowVisible(Hwnd);
        }

        private void EnableDisableClick(object sender, RoutedEventArgs e) {
            WinUser.EnableWindow(Hwnd, !WinUser.IsWindowEnabled(Hwnd));
            e_dButton.IsChecked = WinUser.IsWindowEnabled(Hwnd);
        }

        private void hwndText_TextChanged(object sender, TextChangedEventArgs e) {
            IntPtr hwnd;
            if (IsIntPtr(hwndText.Text, 16) && (hwnd = Hwnd) != IntPtr.Zero && WinUser.IsWindow(hwnd) && hwnd != Handle) {
                s_hButton.IsEnabled = true;
                s_hButton.IsChecked = WinUser.IsWindowVisible(hwnd);
                e_dButton.IsEnabled = true;
                e_dButton.IsChecked = WinUser.IsWindowEnabled(hwnd);

                gotoNextButton.IsEnabled = NextWindow(hwnd) != IntPtr.Zero ? true : false;
                gotoPrevButton.IsEnabled = PrevWindow(hwnd) != IntPtr.Zero ? true : false;
                AddHandle(hwnd);

                StringBuilder strb = new StringBuilder(256);
                WinUser.GetWindowText(hwnd, strb, 256);
                string title = strb.ToString();
                this.Title = string.Format("{0} - {1}", assemblyName.Name, title);
            } else WhenNullHandle();
        }

        private string GetWindowToString(IntPtr hwnd) {
            //if (!WinUser.IsWindow(hwnd))
            //    return null;
            //StringBuilder strb = new StringBuilder(256);
            //WinUser.GetWindowText(hwnd, strb, 256);
            //string title = strb.ToString();
            string title = GetControlText(hwnd);

            StringBuilder sb = new StringBuilder(256);
            WinUser.GetClassName(hwnd, sb, 256);
            string className = sb.ToString();

            string str_hwnd = Convert.ToString(hwnd.ToInt32(), 16).ToUpper();
            while (str_hwnd.Length < 8) str_hwnd = "0" + str_hwnd;

            string item = string.Format("{0} \"{1}\" {2}", str_hwnd, title, className);
            return item;
        }

        public string GetControlText(IntPtr hWnd) {
            StringBuilder title = new StringBuilder(256);

            // Get the size of the string required to hold the window title. 
            Int32 size = WinUser.SendMessage(hWnd, (int)WindowMessage.WM_GETTEXTLENGTH, 0, 0);

            // If the return is 0, there is no title. 
            if (size > 0) {
                title = new StringBuilder(size + 1);
                WinUser.SendMessage(hWnd, (int)WindowMessage.WM_GETTEXT, title.Capacity, title);
            } else
                WinUser.GetWindowText(hWnd, title, title.Capacity);

            return title.ToString();
        }

        private void AddHandle(IntPtr hwnd) {
            if (!WinUser.IsWindow(hwnd) || hwnd == Handle) return;
            string item = GetWindowToString(hwnd);

            if (WinUser.IsWindow(hwnd)) {
                var source = HwndSource.FromHwnd(hwnd);
                if (source != null) {
                    dynamic WpfWindow = source.RootVisual;

                    item = "WPF " + item;
                }
            }

            if (!hwndlist.Items.Contains(item))
                hwndlist.Items.Add(item);
            hwndlist.SelectedItem = item;
            hwndlist.BringIntoView();
        }

        private void WhenNullHandle() {
            s_hButton.IsEnabled = false;
            s_hButton.IsChecked = false;
            e_dButton.IsEnabled = false;
            e_dButton.IsChecked = false;
            gotoNextButton.IsEnabled = false;
            gotoPrevButton.IsEnabled = false;
            this.Title = assemblyName.Name;
        }

        private void search_MouseDown(object sender, MouseButtonEventArgs e) {
            isSearching = true;
            wm = new Window() {
                ShowInTaskbar = false,
                AllowsTransparency = true,
                WindowStyle = WindowStyle.None,
                Width = 0,
                Height = 0,
                Left = int.MaxValue,
                Top = int.MaxValue,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = System.Windows.Media.Brushes.Red,
                BorderThickness = new Thickness(2),
                Topmost = true,
            };
            wm.Show();
            mh.Start();
            wmHandle = new WindowInteropHelper(wm).Handle;

            string path = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\cross.cur";
            this.Cursor = new Cursor(path);
        }

        private void search_MouseUp(object sender, MouseButtonEventArgs e) {
            isSearching = false;
            wm.Close();
            mh.Stop();
            this.Cursor = Cursors.Arrow;
        }

        private void search_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (isSearching) {
                var p = GlobalMouse.Position;
                last_p = p;
                IntPtr _hwnd = WinUser.WindowFromPoint(new MS.Interop.WinUser.POINT() { x = (int)p.X, y = (int)p.Y });
                if (GetControlText(_hwnd).Contains("SVG Code")) {
                    int t = 1;
                }
                if (WinUser.GetAncestor(_hwnd, 1) != DesktopHandle) {
                    var next = NextWindow(_hwnd);
                    while (next != IntPtr.Zero && WinUser.IsWindowVisible(next)) {
                        WinUser.Rect _rect;
                        WinUser.GetWindowRect(next, out _rect);
                        if (p.X >= _rect.Left && p.X <= _rect.Right && p.Y >= _rect.Top && p.Y <= _rect.Bottom) {
                            _hwnd = next;
                        }
                        next = NextWindow(next);
                    }
                }

                WinUser.Rect rect;
                WinUser.GetWindowRect(_hwnd, out rect);
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                int x_rel = (int)(p.X - rect.Left);
                int y_rel = (int)(p.Y - rect.Top);

                if (last_hwnd == _hwnd || _hwnd == wmHandle || _hwnd == cpHandle) return;
                last_hwnd = _hwnd;
                status.Text = string.Format("({0}, {1})-({2}, {3}) {4}x{5}",
                    rect.Left, rect.Top, rect.Right, rect.Bottom,
                    rect.Right - rect.Left, rect.Bottom - rect.Top);

                wm.Left = rect.Left;
                wm.Top = rect.Top;
                wm.Width = rect.Right - rect.Left;
                wm.Height = rect.Bottom - rect.Top;
                if (WinUser.IsMaximize(_hwnd)) {
                    wm.Left = 0;
                    wm.Top = 0;
                    wm.Width = rect.Right + rect.Left;
                    wm.Height = rect.Bottom + rect.Top;
                }

                string str_hwnd = Convert.ToString(_hwnd.ToInt32(), 16).ToUpper();
                while (str_hwnd.Length < 8) str_hwnd = "0" + str_hwnd;
                this.hwndText.Text = str_hwnd;
            }
        }

        private void picker_MouseDown(object sender, MouseButtonEventArgs e) {
            isPickering = true;
            cp = new ColorPicker() {
                Left = int.MaxValue,
                Top = int.MaxValue,
                Topmost = true
            };
            cp.Show();
            mh.Start();
            cpHandle = new WindowInteropHelper(cp).Handle;
            string path = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\cross.cur";
            this.Cursor = new Cursor(path);
        }

        private void picker_MouseUp(object sender, MouseButtonEventArgs e) {
            isPickering = false;
            if (cp != null)
                cp.Close();
            mh.Stop();
            this.Cursor = Cursors.Arrow;
        }

        private void picker_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (isPickering) {
                var p = GlobalMouse.Position;
                int offset = 50;
                if (cp != null) {
                    bmp = new System.Drawing.Bitmap(100, 100, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    System.Drawing.Graphics gph = System.Drawing.Graphics.FromImage(bmp);
                    gph.CopyFromScreen((int)p.X - offset, (int)p.Y - offset, 0, 0, new System.Drawing.Size(100, 100));

                    var color = bmp.GetPixel(offset, offset);
                    pixelColor.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
                    gph.Dispose();

                    double eX = p.X;
                    double eY = p.Y;
                    double X = eX + 10, Y = eY + 10;
                    if (X + cp.Width + 10 > SystemParameters.PrimaryScreenWidth)
                        X = eX - (cp.Width + 10);
                    if (Y + cp.Height + 10 > SystemParameters.PrimaryScreenHeight)
                        Y = eY - (cp.Height + 10);
                    cp.Left = X;
                    cp.Top = Y;
                    int w = ((88 - pixel) / 2 / pixel * pixel * 2) + pixel;
                    int h = ((88 - pixel) / 2 / pixel * pixel * 2) + pixel;
                    ChangePosition(offset, offset, w, h, pixel);
                }
            }
        }

        private void ChangePosition(int x_rel, int y_rel, int w = 88, int h = 88, int pxs = 8) {
            int lt = (w / pxs) / 2; int rb = (h / pxs) / 2;
            var p = new System.Windows.Point(x_rel, y_rel);

            var rpos = p - new System.Windows.Point((int)lt, (int)rb);
            cp.Location = p;
            cp.Zoom = pxs;
            var image = bmp.Clone() as System.Drawing.Bitmap;
            var clr = image.GetPixel(x_rel, y_rel);
            cp.Color = System.Windows.Media.Color.FromRgb(clr.R, clr.G, clr.B);
            var _bmp = new System.Drawing.Bitmap(w, h);
            for (int x = (int)rpos.X; x <= rpos.X + (int)lt * 2; x++)
                for (int y = (int)rpos.Y; y <= rpos.Y + (int)rb * 2; y++) {
                    if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
                        _bmp.SetPixel(Math.Abs((x - (int)rpos.X) * pxs), Math.Abs((y - (int)rpos.Y) * pxs), System.Drawing.Color.Black);
                    else
                        for (int i = (x - (int)rpos.X) * pxs; i < (x - rpos.X) * pxs + pxs; i++)
                            for (int j = (y - (int)rpos.Y) * pxs; j < (y - rpos.Y) * pxs + pxs; j++)
                                try {
                                    _bmp.SetPixel(i, j, image.GetPixel(x, y));
                                } catch { }
                }
            BitmapImage bmpI = new BitmapImage();
            bmpI.BeginInit();
            MemoryStream memoryStream = new MemoryStream();
            _bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
            bmpI.StreamSource = memoryStream;
            bmpI.EndInit();
            cp.Source = bmpI;
        }

        private void hexa_MouseDown(object sender, MouseButtonEventArgs e) {
            System.Windows.Clipboard.SetText(hexa.Text);

            //UILista<int> a = new UILista<int>();
            //if (a.Count == 0)
            //    a.Add(10);
            //a.Add(12);
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var item = ((FrameworkElement)sender).DataContext.ToString();
            var str_hwnd = Regex.Match(item, "[0-9A-F]{8}").Value;
            if (WinUser.IsWindow(new IntPtr(Convert.ToInt32(str_hwnd, 16)))) {
                hwndText.Text = str_hwnd;
            } else hwndlist.Items.Remove(item);
        }

        private void highlight_Click(object sender, RoutedEventArgs e) {
            HighlightedWindow(SelectedHandle);
        }

        private void HighlightedWindow(IntPtr hwnd) {
            wm = new HighlightedWindow() {
                Width = 0,
                Height = 0,
                Left = int.MaxValue,
                Top = int.MaxValue,
                Background = Brushes.Transparent,// new SolidColorBrush(Color.FromArgb(0xa2, 0x12, 0x0a, 0xd0)),
                BorderBrush = System.Windows.Media.Brushes.Red,
                BorderThickness = new Thickness(2),
            };
            wm.Show();

            WinUser.Rect rect;
            WinUser.GetWindowRect(hwnd, out rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            wm.Left = rect.Left;
            wm.Top = rect.Top;
            wm.Width = rect.Right - rect.Left;
            wm.Height = rect.Bottom - rect.Top;
            if (WinUser.IsMaximize(hwnd)) {
                wm.Left = 0;
                wm.Top = 0;
                wm.Width = rect.Right + rect.Left;
                wm.Height = rect.Bottom + rect.Top;
            }

            Thread.Sleep(200);
            wm.Close();
        }

        private void hwndtree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            var tvi = e.NewValue as TreeViewItem;
            if (tvi.Header == null) return;
            var item = tvi.Header.ToString();
            var str_hwnd = Regex.Match(item, "[0-9A-F]{8}").Value;
            if (str_hwnd == "") return;
            if (WinUser.IsWindow(new IntPtr(Convert.ToInt32(str_hwnd, 16)))) {
                hwndText.Text = str_hwnd;
            } else hwndlist.Items.Remove(item);
        }
    }

    public class UILista<T> : IList<T> {
        private UIListElement<T> _first;
        private UIListElement<T> _last;
        private int _cont;

        public class UIListElement<T> : IDisposable {
            public UIListElement<T> Prev;
            public UIListElement<T> Next;
            public UIListElement() { }

            public T At { get; set; }

            ~UIListElement() {
                Prev = null;
                Next = null;
            }

            public void Dispose() {
                Prev = null;
                Next = null;
            }
        }

        public UILista() { }

        public void Add(T element) {
            UIListElement<T> _at = new UIListElement<T>() { At = element };
            _cont++;
            if (_last == null) {
                _last = _at;
                _first = _at;
                return;
            }

            _last.Next = _at;
            _at.Prev = _last;
            _last = _at;
        }

        public UIListElement<T> ElementAt(int index) {
            int indx = 1;
            UIListElement<T> value;
            if (index < Count) {
                value = _first;
                while (indx <= index) {
                    value = _first.Next;
                    indx++;
                }
                return value;
            }
            throw new RankException();
        }

        public int IndexOf(T item) {
            UIListElement<T> value = _first;

            for (int i = 0; i < Count; i++) {
                if (value.At.Equals(item))
                    return i;
                else value = _first.Next;
            }
            return -1;
        }

        public void Insert(int index, T item) {
            if (index == Count) {
                Add(item);
                return;
            }
            var value = new UIListElement<T>() { At = item };
            var at = ElementAt(index);
            value.Prev = at.Prev;
            value.Next = at;
            at.Prev.Next = value;
            at.Prev = value;
        }

        public void RemoveAt(int index) {
            var at = ElementAt(index);
            at.Prev.Next = at.Next;
            at.Next.Prev = at.Prev;
            at.Dispose();
        }

        public void Clear() {
            throw new NotImplementedException();
        }

        public bool Contains(T item) {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public bool Remove(T item) {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator() {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

        public int Count { get { return _cont; } }

        public bool IsReadOnly => throw new NotImplementedException();

        public T this[int index] {
            get {
                return ElementAt(index).At;
            }

            set {
                ElementAt(index).At = value;
            }
        }
    }
}
