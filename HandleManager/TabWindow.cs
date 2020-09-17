using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace HandleManager {
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(TabItem))]
    [TemplatePart(Name = "PART_SelectedContentHost", Type = typeof(ContentPresenter))]
    public class TabWindow : TabControl, IDisposable {

        public TabWindow() {
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e) {
            if (this.Items != null) {
                foreach (var i in e.NewItems) {
                    if (i is Window == false && i.ToString() != "Microsoft.VisualStudio.DesignTools.WpfDesigner.InstanceBuilders.WindowInstance") {
                        throw new TypeLoadException(
                            string.Format("{0}: [{1}]",
                            new TypeLoadException().Message,
                            i));
                    }
                }
            }
            base.OnItemsChanged(e);
        }

        public void Dispose() {
            foreach (Window i in Items) {
                i.Close();
            }
        }

        ~TabWindow() {
            try {
                foreach (Window i in Items) {
                    i.Close();
                }
            } catch { }
        }
    }
}