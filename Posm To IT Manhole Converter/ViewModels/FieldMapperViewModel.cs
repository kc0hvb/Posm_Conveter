using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;

using PicAx_To_IT_Converter;

namespace PicAx_To_IT_Converter.ViewModels {
    public class FieldMapperViewModel : INotifyPropertyChanged {

        private XmlSerializer _xmlSerializer = new XmlSerializer(typeof(PosmToITFieldMapping));
        private string pickConnString;
        private string itpipesConnString;

        private List<string> _tableTypes = new List<string>() { "Asset", "Inspection", "Observation" };
        public List<string> TableTypes {
            get {
                return _tableTypes;
            }
            set {
                _tableTypes = value;
                NotifyPropertyChanged(nameof(TableTypes));
            }
        }

        private string _selectedTableType;
        public string SelectedTableType {
            get {
                return _selectedTableType;
            }
            set {
                _selectedTableType = value;
                NotifyPropertyChanged(nameof(SelectedTableType));
                setVisibleCollectionsToTableType();
            }
        }

        private void setVisibleCollectionsToTableType() {

            if (SelectedTableType == null) {
                return;
            }

            if (PosmMainline == null || PosmInspection == null || PosmObservation == null) {
                populateViewModelLists();
            }

            if (SelectedTableType == "Asset") {
                ActivePosmColumnCollection = PosmMainline;
                ActiveITpipesColumnCollection = MappedValuesML;
            }
            else if (SelectedTableType == "Inspection") {

                ActivePosmColumnCollection = PosmInspection;
                ActiveITpipesColumnCollection = MappedValuesMLI;
            }
            else if (SelectedTableType == "Observation") {

                ActivePosmColumnCollection = PosmObservation;
                ActiveITpipesColumnCollection = MappedValuesMLO;
            }
            
        }


        private ObservableCollection<posmSampleObject> _activePosmColumnCollection;
        public ObservableCollection<posmSampleObject> ActivePosmColumnCollection {
            get {
                return _activePosmColumnCollection;
            }
            set {
                _activePosmColumnCollection = value;
                NotifyPropertyChanged(nameof(ActivePosmColumnCollection));
            }
        }

        private ObservableCollection<MappingValue> _activeITpipesColumnCollection;
        public ObservableCollection<MappingValue> ActiveITpipesColumnCollection {
            get {
                return _activeITpipesColumnCollection;
            }
            set {
                _activeITpipesColumnCollection = value;
                NotifyPropertyChanged(nameof(ActiveITpipesColumnCollection));
            }
        }

        private MappingValue _selectedITField;
        public MappingValue SelectedITField {
            get {
                return _selectedITField;
            }
            set {
                _selectedITField = value;
                NotifyPropertyChanged(nameof(SelectedITField));
            }
        }
        
        private ObservableCollection<posmSampleObject> _posmMainline;
        public ObservableCollection<posmSampleObject> PosmMainline {
            get {
                return _posmMainline;
            }
            set {
                _posmMainline = value;
                NotifyPropertyChanged(nameof(PosmMainline));
            }
        }

        private ObservableCollection<posmSampleObject> _posmInspection;
        public ObservableCollection<posmSampleObject> PosmInspection {
            get {
                return PosmMainline;
            }
            set {
                PosmMainline = value;
                NotifyPropertyChanged(nameof(PosmMainline));
            }
        }

        private ObservableCollection<posmSampleObject> _posmObservation;
        public ObservableCollection<posmSampleObject> PosmObservation {
            get {
                return _posmObservation;
            }
            set {
                _posmObservation = value;
                NotifyPropertyChanged(nameof(PosmObservation));
            }
        }

        private ObservableCollection<MappingValue> _mappedValuesML;
        public ObservableCollection<MappingValue> MappedValuesML {
            get {
                return _mappedValuesML;
            }
            set {
                _mappedValuesML = value;
                NotifyPropertyChanged(nameof(MappedValuesML));
            }
        }

        private ObservableCollection<MappingValue> _mappedValuesMLI;
        public ObservableCollection<MappingValue> MappedValuesMLI {
            get {
                return _mappedValuesMLI;
            }
            set {
                _mappedValuesMLI = value;
                NotifyPropertyChanged(nameof(MappedValuesMLI));
            }
        }

        private ObservableCollection<MappingValue> _mappedValuesMLO;
        public ObservableCollection<MappingValue> MappedValuesMLO {
            get {
                return _mappedValuesMLO;
            }
            set {
                _mappedValuesMLO = value;
                NotifyPropertyChanged(nameof(MappedValuesMLO));
            }
        }


        public void InitializeControl(string posmConnString, string itpipesConnString) {

            pickConnString = posmConnString;
            this.itpipesConnString = itpipesConnString;

        }

        private void populateViewModelLists() {
            
            populateViewModelListsML();
            populateViewModelListsMLI();
            populateViewModelListsMLO();
        }


        private void populateViewModelListsMLO() {
            try
            {
                if (PosmObservation == null)
                {
                    List<posmSampleObject> posmObservationFields = new List<posmSampleObject>();

                    using (OleDbConnection pickConn = new OleDbConnection(pickConnString))
                    using (OleDbCommand pickCommand = pickConn.CreateCommand())
                    using (DataTable pickFieldsMLo = new DataTable())
                    {

                        pickConn.Open();
                        pickCommand.CommandText = "SELECT * FROM [Data] INNER JOIN [FaultCodes] ON [FaultCodes].FaultCodeID = [Data].FaultCodeID;"; // so we can get sample data for the mappings...

                        pickFieldsMLo.Load(pickCommand.ExecuteReader());

                        foreach (DataColumn curColumn in pickFieldsMLo.Columns)
                        {

                            object sampleValue = null;
                            foreach (DataRow curRow in pickFieldsMLo.Rows)
                            {

                                if (curRow[curColumn] != DBNull.Value && !string.IsNullOrWhiteSpace(curRow[curColumn].ToString()))
                                {
                                    sampleValue = curRow[curColumn].ToString();
                                    break;
                                }
                            }
                            posmObservationFields.Add(new posmSampleObject()
                            {
                                DataTypeString = curColumn.DataType.ToString(),
                                FieldName = curColumn.ColumnName,
                                SampleValue = sampleValue?.ToString()
                            });
                        }
                    }

                    ObservableCollection<posmSampleObject> returnPosmObjects = new ObservableCollection<posmSampleObject>();

                    posmObservationFields.Sort((x, y) => string.Compare(x.FieldName, y.FieldName));
                    foreach (var curItem in posmObservationFields)
                    {
                        returnPosmObjects.Add(curItem);
                    }

                    PosmObservation = returnPosmObjects;
                }


                if (MappedValuesMLO == null)
                {

                    List<MappingValue> itpipesMloMappings = new List<MappingValue>();

                    using (OleDbConnection curConn = new OleDbConnection(itpipesConnString))
                    using (OleDbCommand curCommand = curConn.CreateCommand())
                    using (DataTable mlTable = new DataTable())
                    {

                        curConn.Open();
                        curCommand.CommandText = "SELECT TOP 1 * FROM MHO WHERE 1 = 0";

                        mlTable.Load(curCommand.ExecuteReader());

                        foreach (DataColumn curColumn in mlTable.Columns)
                        {

                            itpipesMloMappings.Add(new MappingValue()
                            {
                                itpipesFieldName = curColumn.ColumnName,
                                itpipesTableName = "MHO",
                                posmFields = new ObservableCollection<string>(),
                                posmTableName = "[Data]"
                            });
                        }
                    }

                    itpipesMloMappings.Sort((x, y) => string.Compare(x.itpipesFieldName, y.itpipesFieldName));

                    ObservableCollection<MappingValue> orderedObservableCollectionOfMLO = new ObservableCollection<MappingValue>();

                    foreach (MappingValue curValue in itpipesMloMappings)
                    {
                        orderedObservableCollectionOfMLO.Add(curValue);
                    }
                    MappedValuesMLO = orderedObservableCollectionOfMLO;
                }
            }
            catch(Exception ex)
            {
               
            }
        }

        private void populateViewModelListsMLI() {
            try
            {
                if (PosmInspection == null)
                {
                    List<posmSampleObject> posmInspectionFields = new List<posmSampleObject>();

                    using (OleDbConnection pickConn = new OleDbConnection(pickConnString))
                    using (OleDbCommand pickCommand = pickConn.CreateCommand())
                    using (DataTable pickFieldsMLi = new DataTable())
                    {

                        pickConn.Open();
                        pickCommand.CommandText = "SELECT * FROM [Session] WHERE TemplateName = 'Nassco MACP';"; // so we can get sample data for the mappings...

                        pickFieldsMLi.Load(pickCommand.ExecuteReader());

                        foreach (DataColumn curColumn in pickFieldsMLi.Columns)
                        {

                            object sampleValue = null;
                            foreach (DataRow curRow in pickFieldsMLi.Rows)
                            {

                                if (curRow[curColumn] != DBNull.Value && !string.IsNullOrWhiteSpace(curRow[curColumn].ToString()))
                                {
                                    sampleValue = curRow[curColumn].ToString();
                                    break;
                                }
                            }
                            posmInspectionFields.Add(new posmSampleObject()
                            {
                                DataTypeString = curColumn.DataType.ToString(),
                                FieldName = curColumn.ColumnName,
                                SampleValue = sampleValue?.ToString()
                            });
                        }
                    }

                    ObservableCollection<posmSampleObject> returnPosmObjects = new ObservableCollection<posmSampleObject>();

                    posmInspectionFields.Sort((x, y) => string.Compare(x.FieldName, y.FieldName));

                    foreach (var curItem in posmInspectionFields)
                    {
                        returnPosmObjects.Add(curItem);
                    }

                    PosmInspection = returnPosmObjects;
                }

                if (MappedValuesMLI == null)
                {

                    List<MappingValue> itpipesMliMappings = new List<MappingValue>();

                    using (OleDbConnection curConn = new OleDbConnection(itpipesConnString))
                    using (OleDbCommand curCommand = curConn.CreateCommand())
                    using (DataTable mlTable = new DataTable())
                    {

                        curConn.Open();
                        curCommand.CommandText = "SELECT TOP 1 * FROM MHI WHERE 1 = 0";

                        mlTable.Load(curCommand.ExecuteReader());

                        foreach (DataColumn curColumn in mlTable.Columns)
                        {

                            itpipesMliMappings.Add(new MappingValue()
                            {
                                itpipesFieldName = curColumn.ColumnName,
                                itpipesTableName = "MHI",
                                posmFields = new ObservableCollection<string>(),
                                posmTableName = "[Session]"
                            });
                        }
                    }

                    //itpipesMlMappings = itpipesMlMappings.OrderBy(new Func<MappingValue, string>((MappingValue x) => {
                    //    return x.itpipesFieldName;
                    //})) as ObservableCollection<MappingValue>;

                    itpipesMliMappings.Sort((x, y) => string.Compare(x.itpipesFieldName, y.itpipesFieldName));

                    ObservableCollection<MappingValue> orderedObservableCollectionOfMLI = new ObservableCollection<MappingValue>();

                    foreach (MappingValue curValue in itpipesMliMappings)
                    {
                        orderedObservableCollectionOfMLI.Add(curValue);
                    }

                    MappedValuesMLI = orderedObservableCollectionOfMLI;
                }
            }
            catch (Exception ex)
            {
                
            }
        }

        private void populateViewModelListsML() {
            try
            {
                if (PosmMainline == null)
                {
                    List<posmSampleObject> posmInspectionFields = new List<posmSampleObject>();

                    using (OleDbConnection pickConn = new OleDbConnection(pickConnString))
                    using (OleDbCommand pickCommand = pickConn.CreateCommand())
                    using (DataTable pickFieldsML = new DataTable())
                    {

                        pickConn.Open();
                        pickCommand.CommandText = "SELECT * FROM [Session] WHERE TemplateName = 'Nassco MACP'"; // so we can get sample data for the mappings...
                                                                                                                      //OleDbDataReader reader;
                                                                                                                      //reader = pickCommand.ExecuteReader();
                        pickFieldsML.Load(pickCommand.ExecuteReader());

                        foreach (DataColumn curColumn in pickFieldsML.Columns)
                        {

                            object sampleValue = null;
                            foreach (DataRow curRow in pickFieldsML.Rows)
                            {

                                if (curRow[curColumn] != DBNull.Value && !string.IsNullOrWhiteSpace(curRow[curColumn].ToString()))
                                {
                                    sampleValue = curRow[curColumn].ToString();
                                    break;
                                }
                            }
                            posmInspectionFields.Add(new posmSampleObject()
                            {
                                DataTypeString = curColumn.DataType.ToString(),
                                FieldName = curColumn.ColumnName,
                                SampleValue = sampleValue?.ToString()
                            });
                        }
                    }

                    ObservableCollection<posmSampleObject> returnPosmObjects = new ObservableCollection<posmSampleObject>();

                    posmInspectionFields.Sort((x, y) => string.Compare(x.FieldName, y.FieldName));

                    foreach (var curItem in posmInspectionFields)
                    {
                        returnPosmObjects.Add(curItem);
                    }

                    PosmMainline = returnPosmObjects;
                }

                if (MappedValuesML == null)
                {
                    List<MappingValue> itpipesMlMappings = new List<MappingValue>();

                    using (OleDbConnection curConn = new OleDbConnection(itpipesConnString))
                    using (OleDbCommand curCommand = curConn.CreateCommand())
                    using (DataTable mlTable = new DataTable())
                    {

                        curConn.Open();
                        curCommand.CommandText = "SELECT TOP 1 * FROM MH WHERE 1 = 0";

                        mlTable.Load(curCommand.ExecuteReader());

                        foreach (DataColumn curColumn in mlTable.Columns)
                        {

                            itpipesMlMappings.Add(new MappingValue()
                            {
                                itpipesFieldName = curColumn.ColumnName,
                                itpipesTableName = "MH",
                                posmFields = new ObservableCollection<string>(),
                                posmTableName = "[Session]"
                            });
                        }
                    }

                    itpipesMlMappings.Sort((x, y) => string.Compare(x.itpipesFieldName, y.itpipesFieldName));

                    ObservableCollection<MappingValue> orderedObservableCollectionOfML = new ObservableCollection<MappingValue>();

                    foreach (MappingValue curValue in itpipesMlMappings)
                    {
                        orderedObservableCollectionOfML.Add(curValue);
                    }

                    MappedValuesML = orderedObservableCollectionOfML;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Unable to find Database.");
            }
            }
        



        private string getColumnNameFormattedForDragDrop(DataColumn inputColumn, object columnValue) {

            if (columnValue == null) {
                columnValue = "<NULL>";
            }

            return $"{inputColumn.ColumnName} |-| {inputColumn.DataType.ToString()} |-| {columnValue}";
        }

        public void removeMappingFromITField(string itFieldLabel, string childName) {

            foreach (var curField in ActiveITpipesColumnCollection) {

                string targetPosmField = string.Empty;
                if (curField.itpipesFieldName == itFieldLabel) {
                    foreach (string posmField in curField.posmFields) {

                        if (posmField == childName) {
                            targetPosmField = posmField;
                            break;
                        }
                    }

                    curField.posmFields.Remove(targetPosmField);
                    break;
                }
            }

        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        public ICommand LoadMappingFromFileCommand {
            get {
                return new GenericCommandCanAlwaysExecute(loadMapping);
            }
        }

        public ICommand SaveCurrentMappingToFileCommand {
            get {
                return new GenericCommandCanAlwaysExecute(saveMapping);
            }
        }



        private void loadMapping()
        {

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "XML File|*.xml";

            bool? result = dlg.ShowDialog();

            if (result == null || result == false)
            {
                return;
            }

            PosmToITFieldMapping deserializeObject = null;

            using (StreamReader readStream = new StreamReader(dlg.FileName))
            {

                try
                {
                    deserializeObject = _xmlSerializer.Deserialize(readStream) as PosmToITFieldMapping;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Could not read mapping from file.\n\n" + ex.Message + "\n\n" + ex.StackTrace);
                    return;
                }

            }
            try
            {
                List<string> existingPosmAssetInspFields = (from curItem in PosmMainline select curItem.FieldName).ToList();
                List<string> existingPosmObservationFields = (from curItem in PosmObservation select curItem.FieldName).ToList();

                List<KeyValuePair<string, ObservableCollection<string>>> mappedAssetFieldsFound = new List<KeyValuePair<string, ObservableCollection<string>>>();
                List<KeyValuePair<string, ObservableCollection<string>>> mappedInspFieldsFound = new List<KeyValuePair<string, ObservableCollection<string>>>();
                List<KeyValuePair<string, ObservableCollection<string>>> mappedObsFieldsFound = new List<KeyValuePair<string, ObservableCollection<string>>>();

                foreach (MappingValue curValue in deserializeObject.MappingValues)
                {

                    if (curValue.itpipesTableName == "MH")
                    {

                        foreach (MappingValue val in MappedValuesML)
                        {
                            if (val.itpipesTableName == curValue.itpipesTableName &&
                                val.itpipesFieldName == curValue.itpipesFieldName)
                            {

                                val.posmFields.Clear();
                                foreach (string curField in curValue.posmFields)
                                {

                                    if (existingPosmAssetInspFields.Contains(curField, StringComparer.InvariantCultureIgnoreCase))
                                    {
                                        val.posmFields.Add(curField);
                                    }
                                }
                                val.posmTableName = curValue.posmTableName;
                                mappedAssetFieldsFound.Add(new KeyValuePair<string, ObservableCollection<string>>(val.posmTableName, val.posmFields));
                            }
                        }
                    }
                    else if (curValue.itpipesTableName == "MHI")
                    {

                        foreach (MappingValue val in MappedValuesMLI)
                        {
                            if (val.itpipesTableName == curValue.itpipesTableName &&
                                val.itpipesFieldName == curValue.itpipesFieldName)
                            {

                                val.posmFields.Clear();
                                foreach (string curField in curValue.posmFields)
                                {
                                    if (existingPosmAssetInspFields.Contains(curField, StringComparer.InvariantCultureIgnoreCase))
                                    {
                                        val.posmFields.Add(curField);
                                    }
                                }
                                val.posmTableName = curValue.posmTableName;
                                mappedInspFieldsFound.Add(new KeyValuePair<string, ObservableCollection<string>>(val.posmTableName, val.posmFields));
                            }
                        }
                    }
                    else if (curValue.itpipesTableName == "MHO")
                    {

                        foreach (MappingValue val in MappedValuesMLO)
                        {
                            if (val.itpipesTableName == curValue.itpipesTableName &&
                                val.itpipesFieldName == curValue.itpipesFieldName)
                            {

                                val.posmFields.Clear();
                                foreach (string curField in curValue.posmFields)
                                {
                                    if (existingPosmObservationFields.Contains(curField, StringComparer.InvariantCultureIgnoreCase))
                                    {
                                        val.posmFields.Add(curField);
                                    }
                                }

                                val.posmTableName = curValue.posmTableName;
                                mappedObsFieldsFound.Add(new KeyValuePair<string, ObservableCollection<string>>(val.posmTableName, val.posmFields));
                            }
                        }
                    }
                }

                var mappedPAAssetFields = (
                    from posmSampleObject curPickValueObj in PosmMainline
                    from KeyValuePair<string, ObservableCollection<string>> curKey in mappedAssetFieldsFound
                    from string curFieldName in curKey.Value
                    where curFieldName.Equals(curPickValueObj.FieldName, StringComparison.InvariantCultureIgnoreCase)
                    select curPickValueObj);

                foreach (var curPAField in mappedPAAssetFields)
                {
                    curPAField.IsMapped = true;
                }

                var mappedPAInspFields = (
                    from posmSampleObject curPickValueObj in PosmInspection
                    from KeyValuePair<string, ObservableCollection<string>> curKey in mappedInspFieldsFound
                    from string curFieldName in curKey.Value
                    where curFieldName.Equals(curPickValueObj.FieldName, StringComparison.InvariantCultureIgnoreCase)
                    select curPickValueObj);

                foreach (var curPAField in mappedPAInspFields)
                {
                    curPAField.IsMapped = true;
                }

                var mappedPAObsFields = (
                    from posmSampleObject curPickValueObj in PosmObservation
                    from KeyValuePair<string, ObservableCollection<string>> curKey in mappedObsFieldsFound
                    from string curFieldName in curKey.Value
                    where curFieldName.Equals(curPickValueObj.FieldName, StringComparison.InvariantCultureIgnoreCase)
                    select curPickValueObj);

                foreach (var curPAField in mappedPAObsFields)
                {
                    curPAField.IsMapped = true;
                }


                SelectedTableType = null;
                SelectedTableType = "Asset";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Please Select what you would like to map.");
                return;
            }
        }

    private void saveMapping() {

            PosmToITFieldMapping saveMap = GetMappingObject();

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "XML File|*.xml";
            dlg.AddExtension = true;
            dlg.DefaultExt = ".xml";
            bool? result = dlg.ShowDialog();

            if (result != null && result == true) {

                using (StreamWriter saveWriter = new StreamWriter(dlg.FileName, false)) {

                    _xmlSerializer.Serialize(saveWriter, saveMap);
                }

                System.Windows.MessageBox.Show("Saved Successfully");
            }
        }


        public PosmToITFieldMapping GetMappingObject() {

            PosmToITFieldMapping saveMap = new PosmToITFieldMapping();

            //using a synchronous foreach to preserve object order.
            foreach (MappingValue curMapping in MappedValuesML) {
                saveMap.MappingValues.Add(curMapping);
            }

            foreach (MappingValue curMapping in MappedValuesMLI) {
                saveMap.MappingValues.Add(curMapping);
            }

            foreach (MappingValue curMapping in MappedValuesMLO) {
                saveMap.MappingValues.Add(curMapping);
            }

            return saveMap;
        }
    }

    public class posmSampleObject : INotifyPropertyChanged {

        private string _fieldName;
        public string FieldName {
            get {
                return _fieldName;
            }
            set {
                _fieldName = value;
                NotifyPropertyChanged(nameof(FieldName));
            }
        }

        private string _dataTypeString;
        public string DataTypeString {
            get {
                return _dataTypeString;
            }
            set {
                _dataTypeString = value;
                NotifyPropertyChanged(nameof(DataTypeString));
            }
        }

        private string _sampleValue;
        public string SampleValue {
            get {
                return _sampleValue;
            }
            set {
                _sampleValue = value;
                NotifyPropertyChanged(nameof(SampleValue));
            }
        }

        private bool _isMapped;
        public bool IsMapped {
            get {
                return _isMapped;
            }
            set {
                _isMapped = value;
                NotifyPropertyChanged(nameof(IsMapped));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
