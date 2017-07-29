using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PicAx_To_IT_Converter.ViewModels;

namespace PicAx_To_IT_Converter {
    class ViewModel : INotifyPropertyChanged {

        private bool _isImperialCheckBox = true;
        public static bool isImperialCheckBox = true;

        public bool isImperialCheck
        {
            get
            {
                return _isImperialCheckBox;
            }
            set
            {
                _isImperialCheckBox = value;
                isImperialCheckBox = _isImperialCheckBox;
                NotifyPropertyChanged(nameof(isImperialCheck));
            }
        }

        private string _pathToPosmDatabase = @"C:\IT Projects\Belmont\Belmont Original Data\Other - Hansen Hiccups\POSM.MDB";

        public string PathToPosmDatabase {
            get {
                return _pathToPosmDatabase;
            }
            set {
                _pathToPosmDatabase = value;
                NotifyPropertyChanged(nameof(PathToPosmDatabase));
            }
        }

        private string _pathToITpipesDb = @"C:\IT Projects\Belmont_Manhole_Converted\Belmont_Manhole_Converted.mdb";

        public string PathToITpipesDb {
            get {
                return _pathToITpipesDb;
            }
            set {
                _pathToITpipesDb = value;
                NotifyPropertyChanged(nameof(PathToITpipesDb));
            }
        }

        private bool _canInteractWithUI = true;

        public bool CanInteractWithUI {
            get {
                return _canInteractWithUI;
            }
            set {
                _canInteractWithUI = value;
                NotifyPropertyChanged(nameof(CanInteractWithUI));
            }
        }

        
        private ObservableCollection<string> _errorLog = new ObservableCollection<string>();
        public ObservableCollection<string> ErrorLog {
            get {
                return _errorLog;
            }
            set {
                _errorLog = value;
                NotifyPropertyChanged(nameof(ErrorLog));
            }
        }

        private ObservableCollection<string> _conversionStatus = new ObservableCollection<string>();
        public ObservableCollection<string> ConversionStatus {
            get {
                return _conversionStatus;
            }
            set {
                _conversionStatus = value;
                NotifyPropertyChanged(nameof(ConversionStatus));
            }
        }
        private double _inspectionsConverted;
        public double InspectionsConvertedCount {
            get {
                return _inspectionsConverted;
            }
            set {
                if (value != _inspectionsConverted) {
                    _inspectionsConverted = value;
                    NotifyPropertyChanged(nameof(InspectionsConvertedCount));
                    recalculateProgressPercent();
                }
            }
        }
        private double _inspectionsToConvertCount;
        public double InspectionsToConvertCount {
            get {
                return _inspectionsToConvertCount;
            }
            set {
                if (value != _inspectionsToConvertCount) {
                    _inspectionsToConvertCount = value;
                    NotifyPropertyChanged(nameof(InspectionsToConvertCount));
                    recalculateProgressPercent();
                }
            }
        }
        private void recalculateProgressPercent() {

            ConversionProgressPercent = InspectionsToConvertCount / InspectionsConvertedCount;
        }
        private double _conversionProgressPercent;
        public double ConversionProgressPercent {
            get {
                return _conversionProgressPercent;
            }
            set {
                if (value != _conversionProgressPercent) {
                    _conversionProgressPercent = value;
                    NotifyPropertyChanged(nameof(ConversionProgressPercent));
                }
            }
        }

        private UserControl _mapControl;
        public UserControl FieldMapperControl {
            get {
                return _mapControl;
            }
            set {
                _mapControl = value;
                NotifyPropertyChanged(nameof(FieldMapperControl));
            }
        }

        private FieldMapperViewModel mapperModel;



        public ICommand BeginConversionCommand {
            get {
                return new GenericCommandCanAlwaysExecute(beginConversionProcess);
            }
        }



        private void beginConversionProcess() {
            
            if (mapperModel == null || mapperModel.MappedValuesML == null || mapperModel.MappedValuesMLI == null || mapperModel.MappedValuesMLO == null) {
                System.Windows.MessageBox.Show("Fields must be mapped before beginning conversion process.");
                return;
            }

            CanInteractWithUI = false;


            //This now checks for a certain table inside the database. If that table does not
            //exist it will error out. 
            //Need to add handling to do multiple files at the same time and add handling to skip
            //invalid databases.

            Thread convertThread = new Thread(new ParameterizedThreadStart((x) => {

                ConverterLogic posmConverter = new ConverterLogic(PathToPosmDatabase, PathToITpipesDb, mapperModel.GetMappingObject());
                posmConverter.DataBaseErrorEncountered += handleDataBaseError;
                posmConverter.DataValueErrorEncountered += handleDataValueError;
                posmConverter.IoErrorEncountered += handleIoError;
                posmConverter.DatabaseSuccessfullyConverted += handleDatabaseSuccessfullyConverted;
                posmConverter.InspectionSuccessfullyConverted += handleSingleInspectionConverted;
                posmConverter.NumberOfInspectionsToConvertDiscovered += handleNumberofInspectionsToConvertDiscovered;
                posmConverter.GeneralStatusUpdate += handleGeneralStatusUpdate;

                posmConverter.BeginConversion();
                CanInteractWithUI = true;
            }));

            convertThread.Name = "PrimaryConversionThread";
            convertThread.Start();
        }

        private void handleDataValueError(object sender, DataValueErrorEventArgs e) {


            App.Current.Dispatcher.Invoke(() => {

                ErrorLog.Add($"Data Value Error encountered in {e.PathToDatabase}. Reason: {e.ThrownException.StackTrace}");
            });
        }

        private void handleDataBaseError(object sender, DataBaseErrorEventArgs e) {

            App.Current.Dispatcher.Invoke(() => {

                ErrorLog.Add($"DataBase Error encountered in {e.ConnectionString}. Query: {e.QueryText}. Reason: {e.ThrownException.StackTrace}");
            });

        }

        private void handleIoError(object sender, IoErrorEventArgs e) {

            App.Current.Dispatcher.Invoke(() => {

                string stackTrace = e.ThrownException.StackTrace == null ? e.ThrownException.Message : e.ThrownException.StackTrace;

                ErrorLog.Add($"IO Error Encountered. Reason: {stackTrace}");
            });
        }

        private void handleDatabaseSuccessfullyConverted(object sender, SuccessfulDatabaseConversionEventArgs e) {


            App.Current.Dispatcher.Invoke(() => {

                CanInteractWithUI = true;
                System.Windows.MessageBox.Show("Completed conversion!");
            });
        }

        private void handleSingleInspectionConverted(object sender, SuccessfulInspectionConversionEventArgs e) {


            App.Current.Dispatcher.Invoke(() => {

                InspectionsConvertedCount += 1;
                ConversionStatus.Add(e.ConversionStatusMessage);
            });
        }

        private void handleNumberofInspectionsToConvertDiscovered(object sender, NumberOfInspectionsToConvertDiscoveredEventArgs e) {


            App.Current.Dispatcher.Invoke(() => {

                InspectionsToConvertCount = e.NumberOfInspectionsToConvert;
            });
        }

        private void handleGeneralStatusUpdate(object sender, GeneralConversionNotificationEventArgs e) {


            App.Current.Dispatcher.Invoke(() => {

                ConversionStatus.Add(e.Message);
            });
        }

        public ICommand GetFieldMapperCommand {
            get {
                return new GenericCommandCanAlwaysExecute(addfieldMapperControl);
            }
        }

        private void addfieldMapperControl() {

            Views.FieldMapper fmapper = new Views.FieldMapper();
            mapperModel = fmapper.DataContext as FieldMapperViewModel;

            mapperModel.InitializeControl(
                ConverterLogic.addPosmDbPasswordToConnStringIfNecessary(@"Provider=Microsoft.Ace.OLEDB.12.0; Data Source = " + PathToPosmDatabase), 
                @"Provider=Microsoft.Ace.OLEDB.12.0; Data Source = " + PathToITpipesDb);
            FieldMapperControl = fmapper;

        }

        public ICommand GetPosmDatabasePathCommand {
            get {
                return new GenericCommandCanAlwaysExecute(setPosmDbPath);
            }
        }

        private void setPosmDbPath() {

            var dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Multiselect = false;
            //System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            //PathToPosmDatabase = dlg.SelectedPath;
            bool? result = dlg.ShowDialog();
            if (result != false)
            {
                PathToPosmDatabase = dlg.FileName;
            }
        }

        public ICommand GetITpipesDatabasePathCommand {
            get {
                return new GenericCommandCanAlwaysExecute(setPathToITpipesDatabase);
            }
        }

        private void setPathToITpipesDatabase() {

            var dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Multiselect = false;
            bool? result = dlg.ShowDialog();

            if (result != null && result == true) {
                PathToITpipesDb = dlg.FileName;
            }
        }

        public ViewModel() {

            

            


            //throw new NotImplementedException();
        }




        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }



    public class GenericCommandCanAlwaysExecute : ICommand {

        private Action _action;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            _action();
        }

        public GenericCommandCanAlwaysExecute(Action action) {
            _action = action;
        }
    }
}
