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
using PicAx_To_IT_Converter.ViewModels;

namespace PicAx_To_IT_Converter.Views {
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class FieldMapper : UserControl {
        public FieldMapper() {
            InitializeComponent();
        }

        public void PosmML_StartDrag(object sender, MouseEventArgs e) {

            if (e.LeftButton == MouseButtonState.Pressed) {

                TextBlock tb = sender as TextBlock; //can't be null--the only thing sending is a textblock.

                DataObject dragPayload = new DataObject(typeof(string), tb.Text);
                DragDrop.DoDragDrop(this, dragPayload, DragDropEffects.Move | DragDropEffects.Scroll | DragDropEffects.Copy);
            }
        }

        private void MLMap_Drop(object sender, DragEventArgs e) {

            base.OnDrop(e);

            if (e.Data.GetDataPresent(typeof(posmSampleObject))) {

                posmSampleObject posmSampleField = e.Data.GetData(typeof(posmSampleObject)) as posmSampleObject;

                DockPanel mappingObjectPanel = sender as DockPanel;
                MappingValue itMappingValue = mappingObjectPanel.DataContext as MappingValue;
                if (!itMappingValue.posmFields.Contains(posmSampleField.FieldName)) {
                    itMappingValue.posmFields.Add(posmSampleField.FieldName);
                    itMappingValue.NotifyPropertyChanged(nameof(itMappingValue.posmFields));
                    posmSampleField.IsMapped = true;
                }
            }
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e) {

        }

        private void PosmML_StartDrag(object sender, MouseButtonEventArgs e) {

            Label lbl = sender as Label;

            DockPanel parentPanel = lbl.Parent as DockPanel;

            posmSampleObject callingObject = parentPanel.DataContext as posmSampleObject;



            DataObject dragPayload = new DataObject(typeof(posmSampleObject), callingObject);
            DragDrop.DoDragDrop(this, dragPayload, DragDropEffects.Move | DragDropEffects.Scroll | DragDropEffects.Copy);

            e.Handled = true;
        }

        private void RemoveMappedFieldButtonClick(object sender, RoutedEventArgs e) {

            //There has to be a less shitty way of doing this...
            Button sendingButton = sender as Button;
            var parentPanel = VisualTreeHelper.GetParent(sendingButton) as Grid;
            TextBlock parentTB = null;
            foreach (var curChild in parentPanel.Children) {

                if (curChild.GetType() == typeof(TextBlock)) {
                    parentTB = curChild as TextBlock;
                    break;
                }
            }
            UIElement uberParentPanel = VisualTreeHelper.GetParent(parentPanel) as UIElement;
            while (true) {

                uberParentPanel = VisualTreeHelper.GetParent(uberParentPanel) as UIElement;
                if (uberParentPanel as DockPanel != null) {
                    break;
                }
            }


            TextBlock itFieldTB = null;

            foreach (var curChild in (uberParentPanel as DockPanel).Children) {
                if (curChild as TextBlock != null) {
                    itFieldTB = curChild as TextBlock;
                }
            }

            FieldMapperViewModel context = this.DataContext as FieldMapperViewModel;
            context.removeMappingFromITField( itFieldTB.Text, parentTB.Text);
        }

        private void FieldMapList_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {

            if (!e.Handled) {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        private void MouseDownPassThrough(object sender, MouseButtonEventArgs e) {

            e.Handled = false;
        }
    }
}
