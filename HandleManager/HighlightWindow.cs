using MS.Interop.WinUser;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using static MS.Interop.WinUser.WinUser;

namespace HandleManager {
    internal class HighlightedWindow : Window {
        private WindowInteropHelper wih;
        public HighlightedWindow() {
            InitializeWindow();
        }

        public IntPtr Handle { get { return wih != null ? wih.Handle : IntPtr.Zero; } }

        protected override void OnSourceInitialized(EventArgs e) {
            wih = new WindowInteropHelper(this);

            InitInstance();
            base.OnSourceInitialized(e);
        }

        private void InitializeWindow() {
            base.WindowStyle = System.Windows.WindowStyle.None;
            base.AllowsTransparency = true;
            base.Topmost = true;
        }

        private void InitInstance() {
            var _Style = (long)(
                WS_VISIBLE |
                WS_CLIPSIBLINGS |
                WS_CLIPCHILDREN |
                WS_SYSMENU |
                WS_THICKFRAME |
                WS_OVERLAPPED);
            SetWindowLong(this.Handle, (int)GWL_STYLE, _Style);

            var _StyleEx = (long)(
                WS_EX_LEFT |
                WS_EX_LTRREADING |
                WS_EX_RIGHTSCROLLBAR |
                WS_EX_TOPMOST |
                WS_EX_TOOLWINDOW |
                WS_EX_TRANSPARENT |
                WS_EX_WINDOWEDGE |
                WS_EX_LAYERED);
            SetWindowLong(this.Handle, (int)GWL_EXSTYLE, _StyleEx);
        }
    }
}