using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

namespace PicAx_To_IT_Converter {

    public class ConverterLogic {

        public event EventHandler<DataValueErrorEventArgs> DataValueErrorEncountered;
        public event EventHandler<DataBaseErrorEventArgs> DataBaseErrorEncountered;
        public event EventHandler<IoErrorEventArgs> IoErrorEncountered;
        public event EventHandler<SuccessfulDatabaseConversionEventArgs> DatabaseSuccessfullyConverted;
        public event EventHandler<SuccessfulInspectionConversionEventArgs> InspectionSuccessfullyConverted;
        public event EventHandler<NumberOfInspectionsToConvertDiscoveredEventArgs> NumberOfInspectionsToConvertDiscovered;
        public event EventHandler<GeneralConversionNotificationEventArgs> GeneralStatusUpdate;


        //Because these were different fields in the combined data they provided, need to handle either ID or ID2 being the id field
        private string assetIdField;
        private string inspIDField => assetIdField;
        private string obsIdField;

        private string obsFkField;

        private string posmConnString;
        private string itpipesConnString;


        public bool isImperialCheck = ViewModel.isImperialCheckBox;




        private string pathToPosmDb;
        public string PathToPosmData {
            private get {
                return posmConnString;
            }
            set {
                pathToPosmDb = value;
                posmConnString = @"Provider=Microsoft.Ace.OLEDB.12.0; Data Source = " + value;
            }
        }

        private string pathToITDb;
        public string PathToITpipesDataBase {
            private get {
                return itpipesConnString;
            }
            set {
                pathToITDb = value;
                itpipesConnString = @"Provider=Microsoft.Ace.OLEDB.12.0; Data Source = " + value;
            }
        }

        //No dynamic field mapping instituted yet--fields are mapped at the end of this file...




        private PosmToITFieldMapping fieldMapping;


        public ConverterLogic(string pathToPosmDatabase, string pathToITpipesDatabase, PosmToITFieldMapping mapping) {

            PathToPosmData = pathToPosmDatabase;
            PathToITpipesDataBase = pathToITpipesDatabase;
            fieldMapping = mapping; //since the mapping should have been created for this database, we'll assume that all the fields exist for the time being.
            posmConnString = addPosmDbPasswordToConnStringIfNecessary(posmConnString);
        }

        public static string addPosmDbPasswordToConnStringIfNecessary(string input) {

            //By default PicAx databases are password protected, but in the San Bruno data they weren't alwways password protected. Need to adjust for whichever the case may be.
            string returnString = input;

            using (OleDbConnection testConn = new OleDbConnection(returnString)) {

                try {
                    testConn.Open();
                }
                catch (OleDbException olex) {

                    returnString += "; Jet OLEDB:Database Password=TriLambda;";

                    using (OleDbConnection passProtConn = new OleDbConnection(returnString)) {

                        //if we throw an exception here, it's because it really is an invalid database.

                        try {
                            passProtConn.Open();
                        }
                        catch (OleDbException passOlex) {

                            Console.WriteLine("Could not open database--with or without POSM password.");
                            return input;
                        }
                    }
                }
            }
            return returnString;
        }

        private void getNumberOfInspectionsToConvert() {

            using (OleDbConnection curConn = new OleDbConnection(posmConnString))
            using (OleDbCommand curCommand = curConn.CreateCommand()) {

                curConn.Open();
                curCommand.CommandText = "SELECT COUNT(*) FROM [Session]";

                int totalInspections = (int)curCommand.ExecuteScalar();
                NumberOfInspectionsToConvertDiscovered?.Invoke(this, new NumberOfInspectionsToConvertDiscoveredEventArgs(totalInspections));
            }
        }

        private void setUniqueIdFields() {

            using (OleDbConnection curConn = new OleDbConnection(posmConnString)) 
            using (OleDbCommand curCommand = curConn.CreateCommand())
            using (DataTable inspFieldsTable = new DataTable())
            using (DataTable obsFieldsTable = new DataTable()) {

                curConn.Open();

                curCommand.CommandText = "SELECT TOP 1 * FROM [Session] WHERE 1 = 0;";
                inspFieldsTable.Load(curCommand.ExecuteReader());

                curCommand.CommandText = "SELECT TOP 1 * FROM [Data] WHERE 1 = 0;";
                obsFieldsTable.Load(curCommand.ExecuteReader());

                if (inspFieldsTable.Columns.Contains("SessionID")) {
                    assetIdField = "SessionID";
                }
                else {
                    assetIdField = "SessionID";
                }

                if (obsFieldsTable.Columns.Contains("DataID"))
                {
                    obsIdField = "DataID";
                }
                else
                {
                    obsIdField = "DataID";
                }

                //if (obsFieldsTable.Columns.Contains("SessionID")) {
                //    obsFkField = "SessionID";
                //}
                //else {
                //    obsFkField = "SessionID";
                //}
            }
        }

        public void BeginConversion()
        {
                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Verifying that databases are valid POSM databases..."));
                if (!validateDatabaseConnections())
                {
                    return;
                }

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Determining primary key foreign key fields..."));
                setUniqueIdFields();

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Getting number of inspections to convert..."));
                getNumberOfInspectionsToConvert();

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Adding ConversionGUID fields to databases to tie records together post conversion..."));
                addConversionGuidFieldsToDatabases();

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Populating GUIDS in POSM data to uniquely identify records..."));
                populateConversionGuidsInPosmData();

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Adding parent guids to observations and media to tie records together later..."));
                tieParentGuidsToChildRecordsInPosmData();

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Pulling POSM data into in-memory set to begin conversion process..."));
                DataTable posmAssetInspectionTable = getInspectionsTableFromPosm();
                DataTable posmObservationTable = getObservationsTableFromPosm();
                DataTable posmMedia = getAllPosmMediaWithITpipesFieldNames();

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Adding converted POSM rows to ITpipes database..."));
                insertPosmRowsIntoITDatabase(posmAssetInspectionTable, "ML");
                insertPosmRowsIntoITDatabase(posmAssetInspectionTable, "MLI");
                insertPosmRowsIntoITDatabase(posmObservationTable, "MLO");

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Moving media rows into ITpipes database..."));
                insertMediaRowsIntoITPipesDatabase(posmMedia);

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Fixing Blank File Paths in ITpipes database..."));
                fixingMediaRowsIntoITPipesDatabase(posmMedia);

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Fixing Blank File Paths in ITpipes database..."));
                fixingImperialRowsIntoITPipesDatabase(posmMedia);

                GeneralStatusUpdate?.Invoke(this, new GeneralConversionNotificationEventArgs("Linking child rows to parent records in converted data..."));
                linkChildParentRowsInITpipesDatabase();

                DatabaseSuccessfullyConverted?.Invoke(this, new SuccessfulDatabaseConversionEventArgs(PathToPosmData, 0, 0));
            } 
        

        private void linkChildParentRowsInITpipesDatabase() {

            string queryText = "";

            using (OleDbConnection curConn = new OleDbConnection(itpipesConnString))
            using (OleDbCommand curCommand = curConn.CreateCommand()) {

                curConn.Open();

                curCommand.CommandText = queryText = 
                    "UPDATE MLO LEFT JOIN MLI ON MLO.ParentGUID = MLI.ConversionGUID SET MLO.MLI_ID = MLI.MLI_ID;";

                try {
                    curCommand.ExecuteNonQuery();
                }
                catch(OleDbException olex) {
                    DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(itpipesConnString, queryText, olex));
                }

                curCommand.CommandText = queryText = 
                    "UPDATE MLI LEFT JOIN ML ON MLI.ConversionGUID = ML.ConversionGUID SET MLI.ML_ID = ML.ML_ID;"; //in this instance, the conversionGUID is also the parent guid since it comes from the same row.

                try {
                    curCommand.ExecuteNonQuery();
                }
                catch (OleDbException olex) {
                    DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(itpipesConnString, queryText, olex));
                }

                curCommand.CommandText = queryText = 
                    "INSERT INTO MLO_Media (MLO_ID, Media_ID) SELECT MLO_ID, Media_ID FROM MLO LEFT JOIN Media ON MLO.ConversionGUID = Media.ParentGUID WHERE Media.Media_ID IS NOT NULL";

                try {
                    curCommand.ExecuteNonQuery();
                }
                catch (OleDbException olex) {
                    DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(itpipesConnString, queryText, olex));
                }

                curCommand.CommandText = queryText =
                    "INSERT INTO MLI_Media (MLI_ID, Media_ID) SELECT MLI.MLI_ID, Media_ID FROM MLI LEFT JOIN Media ON MLI.ConversionGUID = Media.ParentGUID WHERE Media.Media_ID IS NOT NULL";

                try {
                    curCommand.ExecuteNonQuery();
                }
                catch (OleDbException olex) {
                    DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(itpipesConnString, queryText, olex));
                }
            }

                //throw new NotImplementedException();
        }

        private void insertMediaRowsIntoITPipesDatabase(DataTable posmMedia) {

            using (OleDbConnection curConn = new OleDbConnection(itpipesConnString))
            using (OleDbCommand curCommand = curConn.CreateCommand()) {

                curConn.Open();

                curCommand.CommandText = $"INSERT INTO Media (File_Name, File_Path, File_Type, Media_Path_ID, ParentGUID) VALUES (?, ?, ?, ?, ?)";
                foreach (DataRow curRow in posmMedia.Rows) {

                    curCommand.Parameters.Clear();

                    curCommand.Parameters.AddWithValue("File_Name", curRow["File_Name"]);
                    curCommand.Parameters.AddWithValue("File_Path", curRow["File_Path"]);
                    curCommand.Parameters.AddWithValue("File_Type", curRow["File_Type"]);
                    curCommand.Parameters.AddWithValue("Media_Path_ID", curRow["Media_Path_ID"]);
                    curCommand.Parameters.AddWithValue("ParentGUID", curRow["ParentGUID"]);

                    try {
                        curCommand.ExecuteNonQuery();
                    }
                    catch (OleDbException olex) {
                        DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(itpipesConnString, curCommand.CommandText, olex));
                    }
                }
            }

        }

        private void fixingMediaRowsIntoITPipesDatabase(DataTable posmMedia)
        {

            using (OleDbConnection curConn = new OleDbConnection(itpipesConnString))
            using (OleDbCommand curCommand = curConn.CreateCommand())
            {

                curConn.Open();

                curCommand.CommandText = $"UPDATE Media SET File_Path = '\\' WHERE File_Path IS NULL OR File_Path = ''";
                

                    curCommand.Parameters.Clear();

                    try
                    {
                        curCommand.ExecuteNonQuery();
                    }
                    catch (OleDbException olex)
                    {
                        DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(itpipesConnString, curCommand.CommandText, olex));
                    }
                
            }

        }

        private void fixingImperialRowsIntoITPipesDatabase(DataTable posmMedia)
        {

            using (OleDbConnection curConn = new OleDbConnection(itpipesConnString))
            using (OleDbCommand curCommand = curConn.CreateCommand())
            {

                curConn.Open();

                curCommand.CommandText = $"UPDATE MLI SET IsImperial = 1;";


                curCommand.Parameters.Clear();

                try
                {
                    if (isImperialCheck == true)
                    {
                        curCommand.ExecuteNonQuery();
                    }
                }
                catch (OleDbException olex)
                {
                    DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(itpipesConnString, curCommand.CommandText, olex));
                }
            }
            }

        

        private void insertPosmRowsIntoITDatabase(DataTable posmTable, string itpipesTable) {

            string query = "SELECT * FROM [Session];";

            using (OleDbConnection curConn = new OleDbConnection(itpipesConnString))
            using (OleDbCommand curCommand = curConn.CreateCommand()) {

                curConn.Open();

                foreach (DataRow curRow in posmTable.Rows) {

                    loadCommandWithInsertLogic(curRow, itpipesTable, curCommand);
                    query = curCommand.CommandText;
                    try {
                        curCommand.ExecuteNonQuery();
                        if (itpipesTable.Equals("MLI", StringComparison.InvariantCultureIgnoreCase)) {
                            InspectionSuccessfullyConverted?.Invoke(this, new SuccessfulInspectionConversionEventArgs($"Converted inspection {curRow[inspIDField]}"));
                        }
                    }
                    catch (OleDbException olex) {
                        DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(itpipesConnString, query, olex));
                    }
                }
            }
        }

        private Dictionary<string, Dictionary<string, ObservableCollection<string>>> tableMapDictionary = new Dictionary<string, Dictionary<string, ObservableCollection<string>>>();

        private void loadCommandWithInsertLogic(DataRow curRow, string itpipesTableName, OleDbCommand commandToLoad) {

            if (!tableMapDictionary.ContainsKey(itpipesTableName)) {

                buildFieldMapDictionaryForTable(itpipesTableName);
            }

            commandToLoad.Parameters.Clear();

            Dictionary<string, ObservableCollection<string>> curFieldMapDictionary = tableMapDictionary[itpipesTableName];

            StringBuilder insertScopeString = new StringBuilder($"INSERT INTO {itpipesTableName} (");
            StringBuilder insertParameterString = new StringBuilder(") VALUES (");

            foreach(string curField in curFieldMapDictionary.Keys) {

                insertScopeString.Append($"[{curField}], ");
                ObservableCollection<string> pickFields = curFieldMapDictionary[curField];

                object value = null;
                if (pickFields.Count > 1) {
                    value = combineDataColumnObjectValues(curRow[pickFields[0]], curRow[pickFields[1]]);
                }
                else {
                    value = curRow[pickFields[0]];
                }

                //Memo types get real pissy if you try to add an empty string to them, so empty strings should become null.
                if (value.GetType() == typeof(string) && value != DBNull.Value && value.ToString() == string.Empty) {
                    value = DBNull.Value;
                }

                insertParameterString.Append("?, ");
                commandToLoad.Parameters.AddWithValue(pickFields[0], value);
            }

            if (curRow.Table.Columns.Contains("ParentGUID")) {
                insertScopeString.Append("ParentGUID, ");
                insertParameterString.Append("?, ");
                commandToLoad.Parameters.AddWithValue("ParentGUID", curRow["ParentGUID"]);
            }

            insertScopeString.Append("ConversionGUID");
            insertParameterString.Append("?)");
            commandToLoad.Parameters.AddWithValue("ConversionGUID", curRow["ConversionGUID"]);

            commandToLoad.CommandText = insertScopeString.ToString() + insertParameterString.ToString();
        }

        private object combineDataColumnObjectValues(object obj1, object obj2) {

            if (obj1 == null) {
                if (obj2 == null) {
                    return DBNull.Value;
                }
                return obj2;
            }
            if (obj2 == null) {
                return obj1; //already checkd for both being null
            }

            if (obj1 == DBNull.Value) {
                if (obj2 == DBNull.Value) {
                    return DBNull.Value;
                }
                return obj2;
            }

            if (obj2 == DBNull.Value) {
                return obj1;
            }

            if (obj1.GetType() == typeof(string)) {
                return $"{obj1.ToString().Trim()} {obj2?.ToString().Trim()}".Trim();
            }
            else if (obj1.GetType() == typeof(DateTime) && obj2.GetType() == typeof(DateTime)) {
                //specifically to allow separate date and time fields to be combined. Ludicrous, but some people do it.
                DateTime dt1 = new DateTime();
                DateTime dt2 = new DateTime();

                if (DateTime.TryParse(obj1.ToString(), out dt1) &&
                    DateTime.TryParse(obj2.ToString(), out dt2)) {

                    return dt1.Add(new TimeSpan(dt2.TimeOfDay.Ticks));
                }
            }

            DataValueErrorEncountered?.Invoke(this, new DataValueErrorEventArgs("", new Exception($"Returning obj1 value--The values cannot be combined: {obj1}, {obj2}")));
            
            return obj1;
        }

        private void buildFieldMapDictionaryForTable(string itpipesTableName) {

            var newDictionary = (from MappingValue curField in fieldMapping.MappingValues.AsEnumerable().AsParallel()
                                 where curField.itpipesTableName.Equals(itpipesTableName, StringComparison.InvariantCultureIgnoreCase) && curField.posmFields != null && curField.posmFields.Count > 0
                                 select new { itField = curField.itpipesFieldName, pickFields = curField.posmFields }).ToDictionary((x) => x.itField, (y) => y.pickFields);

            tableMapDictionary.Add(itpipesTableName, newDictionary);
        }

        private DataTable getAllPosmMediaWithITpipesFieldNames() {

            DataTable returnTable = new DataTable();

            string queryText =
                "SELECT VideoLocation AS File_Name, ('\\Video\\' + MediaFolder) AS File_Path, 'Video' AS File_Type, "+
                "1 AS Media_Path_ID, ParentGUID " +
                "FROM Data INNER JOIN [Session] ON [Data].SessionID = [Session].SessionID WHERE FaultCodeID = 1; ";

            using (OleDbConnection pickConn = new OleDbConnection(posmConnString))
            using (OleDbCommand pickCommand = pickConn.CreateCommand()) {

                pickConn.Open();

                pickCommand.CommandText = queryText;
                try {

                    returnTable.Load(pickCommand.ExecuteReader());

                    pickCommand.CommandText = queryText =
                        "SELECT PictureLocation AS File_Name, ('\\Video\\' + MediaFolder + '\\') AS File_Path, 'Snapshot' AS File_Type, 1 "+
                        "AS Media_Path_ID, Data.ConversionGUID AS ParentGUID FROM Data "+
                        "INNER JOIN [Session] ON [Data].SessionID = [Session].SessionID Where FaultCodeID <> 1";

                    returnTable.Load(pickCommand.ExecuteReader());
                }

                catch (Exception ex) {
                    DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(posmConnString, queryText, ex));
                }
            }

            return returnTable;
        }

        private DataTable getPosmObservationMedia() {

            DataTable returnTable = new DataTable();

            string queryText =
                "SELECT VideoLocation " +
                "FROM [Data] WHERE Faultcode = '1'; ";

            using (OleDbConnection pickConn = new OleDbConnection(posmConnString))
            using (OleDbCommand pickCommand = pickConn.CreateCommand()) {

                pickConn.Open();

                pickCommand.CommandText = queryText;
                try {

                    returnTable.Load(pickCommand.ExecuteReader());
                }

                catch (Exception ex) {
                    DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(posmConnString, queryText, ex));
                }
            }

            return returnTable;
        }

        private DataTable getObservationsTableFromPosm() {

            DataTable returnTable = new DataTable();

            string queryText =
                "SELECT * " +
                "FROM [Data] INNER JOIN FaultCodes ON [Data].FaultCodeID = FaultCodes.FaultCodeID " +
                "WHERE ParentGUID IN (Select ConversionGUID FROM [Session]) AND [Data].FaultCodeID <> 1;";

            using (OleDbConnection pickConn = new OleDbConnection(posmConnString))
            using (OleDbCommand pickCommand = pickConn.CreateCommand()) {

                pickConn.Open();

                pickCommand.CommandText = queryText;
                try {

                    returnTable.Load(pickCommand.ExecuteReader());
                }

                catch (Exception ex) {
                    DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(posmConnString, queryText, ex));
                }
            }

            return returnTable;
        }

        private DataTable getInspectionsTableFromPosm() {

            DataTable returnTable = new DataTable();

            string queryText =
                "SELECT * FROM [Session];";

            using (OleDbConnection pickConn = new OleDbConnection(posmConnString))
            using (OleDbCommand pickCommand = pickConn.CreateCommand()) {

                pickConn.Open();

                pickCommand.CommandText = queryText;
                try {

                    returnTable.Load(pickCommand.ExecuteReader());
                }

                catch (Exception ex) {
                    DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(posmConnString, queryText, ex));
                }
            }

            return returnTable;
        }

        private void populateConversionGuidsInPosmData() {

            string queryText = "";

            try {
                using (OleDbConnection pickConn = new OleDbConnection(posmConnString))
                using (DataTable lineInspectionIds = new DataTable())
                using (DataTable lineInspectionConditionIds = new DataTable()) {
                    
                    pickConn.Open();

                    OleDbTransaction curTransaction = pickConn.BeginTransaction();
                    OleDbCommand pickCommand = new OleDbCommand("", pickConn, curTransaction);

                    //these can remain with hard-coded reference to ID as no joining operations are performed.
                    pickCommand.CommandText = queryText = "SELECT [SessionID] FROM [Session] WHERE ConversionGUID IS NULL;";
                    lineInspectionIds.Load(pickCommand.ExecuteReader());

                    pickCommand.CommandText = queryText = "SELECT [DataID] FROM [Data] WHERE ConversionGUID IS NULL";
                    lineInspectionConditionIds.Load(pickCommand.ExecuteReader());

                    Thread inspThread = new Thread(new ParameterizedThreadStart((x) => {
                        
                            foreach (DataRow curRow in lineInspectionIds.Rows) {

                                lock (pickCommand) {
                                    pickCommand.CommandText = queryText = $"UPDATE [Session] SET ConversionGUID = '{Guid.NewGuid().ToString()}' WHERE [SessionID] = {(int)curRow["SessionID"]}";
                                    pickCommand.ExecuteNonQuery();
                                }
                            }
                    }));

                    Thread obsThread = new Thread(new ParameterizedThreadStart((y) => {
                        
                        foreach (DataRow curRow in lineInspectionConditionIds.Rows) {
                            lock (pickCommand) {

                                pickCommand.CommandText = queryText = $"UPDATE [Data] SET ConversionGUID = '{Guid.NewGuid().ToString()}' WHERE [DataID] = {(int)curRow["DataID"]}";
                                pickCommand.ExecuteNonQuery();
                            }
                        }
                    }));
                    
                    inspThread.Name = "InspectionGuidUpdateThread";
                    obsThread.Name = "ObservationGuidUpdateThread";
                    inspThread.Start();
                    obsThread.Start();
                    
                    inspThread.Join();
                    obsThread.Join();


                    curTransaction.Commit();
                    pickCommand.Dispose();
                }
            }
            catch (Exception ex) {
                DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(posmConnString, queryText, ex));
            }
        }

        private void addConversionGuidFieldsToDatabases() {

            //highly probable that errors will be encountered, as the converter is new and will likely have errors crashing it out during conversion.

            
            List<DataBaseErrorEventArgs> errorsEncountered = new List<DataBaseErrorEventArgs>();
            string[] posmTablesToAddGuidsTo = new string[] {
                "[Session]",
                "[Data]"
            };

            string[] posmTablesToAddParentGuid = new string[] {
                "[Data]"
            };

            string[] itpipesTablesToAddForeignGuidColumnsTo = new string[] {
                "ML", "MLI", "MLO", "Media"
            };

            using (OleDbConnection pickConn = new OleDbConnection(posmConnString))
            using (OleDbCommand pickCommand = pickConn.CreateCommand()) {

                pickConn.Open();


                foreach (string curPickTable in posmTablesToAddGuidsTo) {

                    try {
                        string columnExists = "True";
                        try {
                            pickCommand.CommandText = $"SELECT TOP 1 ConversionGUID FROM {curPickTable};";
                            pickCommand.ExecuteNonQuery();
                                } catch { columnExists = "False"; };
                        if (columnExists == "False") {
                            pickCommand.CommandText = $"ALTER TABLE {curPickTable} ADD ConversionGUID varchar(40);";
                            pickCommand.ExecuteNonQuery();
                        } }
                    catch (Exception ex) {
                        errorsEncountered.Add(new DataBaseErrorEventArgs(posmConnString, pickCommand.CommandText, ex));
                    } 
                } 

                foreach (string curTable in posmTablesToAddParentGuid) {

                    
                    try
                    {
                        string columnExists = "True";
                        try
                        {
                            pickCommand.CommandText = $"SELECT TOP 1 ParentGUID FROM {curTable};";
                            pickCommand.ExecuteNonQuery();
                        }
                        catch { columnExists = "False"; };
                        if (columnExists == "False")
                        {
                            pickCommand.CommandText = $"ALTER TABLE {curTable} ADD ParentGUID varchar(40);";
                            pickCommand.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        errorsEncountered.Add(new DataBaseErrorEventArgs(posmConnString, pickCommand.CommandText, ex));
                    }
                }
            }


            using (OleDbConnection curConn = new OleDbConnection(itpipesConnString))
            using (OleDbCommand curCommand = curConn.CreateCommand()) {

                curConn.Open();

                foreach (string curTable in itpipesTablesToAddForeignGuidColumnsTo) {

                    try
                    {
                        string columnExists = "True";
                        try
                        {
                            curCommand.CommandText = $"SELECT TOP 1 ConversionGUID FROM {curTable};";
                            curCommand.ExecuteNonQuery();
                        }
                        catch { columnExists = "False"; };
                        if (columnExists == "False")
                        {
                            curCommand.CommandText = $"ALTER TABLE {curTable} ADD ConversionGUID varchar(40);";
                            curCommand.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {

                        errorsEncountered.Add(new DataBaseErrorEventArgs(posmConnString, curCommand.CommandText, ex));
                    }

                    try
                    {
                        string columnExists = "True";
                        try
                        {
                            curCommand.CommandText = $"SELECT TOP 1 ParentGUID FROM {curTable};";
                            curCommand.ExecuteNonQuery();
                        }
                        catch { columnExists = "False"; };
                        if (columnExists == "False")
                        {

                            curCommand.CommandText = $"ALTER TABLE {curTable} ADD ParentGUID varchar(40);";
                            curCommand.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {

                        errorsEncountered.Add(new DataBaseErrorEventArgs(posmConnString, curCommand.CommandText, ex));
                    }
                }
            }

            foreach (var curError in errorsEncountered) {
                DataBaseErrorEncountered?.Invoke(this, curError);
            }

        }

        private void tieParentGuidsToChildRecordsInPosmData() {

            string queryText = "";


            try {
                using (OleDbConnection pickConn = new OleDbConnection(posmConnString))
                using (OleDbCommand pickCommand = pickConn.CreateCommand()) {

                    pickConn.Open();
                    
                    pickCommand.CommandText = queryText =
                        $"UPDATE [Data] AS obs LEFT JOIN [Session] AS insp ON obs.{inspIDField} = insp.{inspIDField} SET obs.ParentGUID = insp.ConversionGUID;";
                    pickCommand.ExecuteNonQuery();

                    //pickCommand.CommandText = queryText =
                      //  $"UPDATE [Data] AS media LEFT JOIN Data AS insp ON media.ID_LineInspection = insp.{inspIDField} SET media.ParentGUID = insp.ConversionGUID;";
                   // pickCommand.ExecuteNonQuery();

                   // pickCommand.CommandText = queryText =
                       // $"UPDATE Data AS media LEFT JOIN Data AS obs ON media.ID_LineInspectionCondition = obs.{obsIdField} SET media.ParentGUID = obs.ConversionGUID;";
                    //pickCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex) {
                DataBaseErrorEncountered?.Invoke(this, new DataBaseErrorEventArgs(posmConnString, queryText, ex));
            }
        }

        private bool validateDatabaseConnections() {

            try {
                if (!isThisAnITpipesDatabase(pathToITDb)) {
                    IoErrorEncountered?.Invoke(this, new IoErrorEventArgs(new Exception("The ITPipes database supplied is not of a recognized format.")));
                    return false;
                }
                if (!isThisAPosmDatabase(pathToPosmDb)) {

                    IoErrorEncountered?.Invoke(this, new IoErrorEventArgs(new Exception("The Posm database supplied is not of a recognized format.")));
                    return false;
                }
            }
            catch (IOException ioex) {
                IoErrorEncountered?.Invoke(this, new IoErrorEventArgs(ioex));
                return false;
            }
            catch (OleDbException olex) {
                IoErrorEncountered?.Invoke(this, new IoErrorEventArgs(olex));
                return false;
            }

            return true;
        }

        private bool isThisAnITpipesDatabase(string pathToFile) {

            if (pathToFile == null ||
                File.Exists(pathToFile) == false ||
                Path.GetExtension(pathToFile).ToUpper() != ".MDB") {
                return false;
            }

            using (OleDbConnection curOleConn = new OleDbConnection(@"Provider=Microsoft.Ace.OLEDB.12.0; Data Source = " + pathToFile)) {

                curOleConn.Open();

                //T_FileIndexField is used as the unique table to ID ITpipes databases because it's always present and the possibility that another db uses this table name is incomprehensibly low.
                string[] Restrictions = {
                    null, //Catalog
                    null, //Owner
                    "T_File_Index_Field" }; //Table Name

                DataTable schemaTable = curOleConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, Restrictions);

                int tablesFound = schemaTable.Rows.Count;

                schemaTable.Dispose();

                if (tablesFound > 0) {
                    return true;
                }
            }


            return false;
        }

        private bool isThisAPosmDatabase(string pathToDb) {

            string extension = Path.GetExtension(pathToDb).Replace(".", "");

            if (string.IsNullOrWhiteSpace(pathToDb) ||
                !File.Exists(pathToDb) ||
                !new string[] { "accdb", "mdb" }.Contains(extension, StringComparer.InvariantCultureIgnoreCase)) {
                return false;
            }

            string connnectionstring;
            try {
                connnectionstring = addPosmDbPasswordToConnStringIfNecessary(@"Provider=Microsoft.Ace.OLEDB.12.0; Data Source = " + pathToDb);
            }
            catch {
                return false;
            }

            using (OleDbConnection curOleConn = new OleDbConnection(connnectionstring)) {

                curOleConn.Open();

                //FaultCodes is used as the unique table as the possibility that another db uses this table name is incomprehensibly low.
                string[] Restrictions = {
                    null, //Catalog
                    null, //Owner
                    "FaultCodes" }; //Table Name

                DataTable schemaTable = curOleConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, Restrictions);

                int tablesFound = schemaTable.Rows.Count;

                schemaTable.Dispose();

                if (tablesFound > 0) {
                    return true;
                }
            }

            return false;
        }
               
    }
}
