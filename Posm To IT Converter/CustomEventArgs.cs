using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicAx_To_IT_Converter {
    
    public class DataValueErrorEventArgs : EventArgs {

        public DataValueErrorEventArgs(string pathToDb, Exception ex, object sourceState = null, object targetState = null) {

            PathToDatabase = pathToDb;
            ThrownException = ex;
            SourceState = sourceState;
            TargetState = targetState;
        }

        public string PathToDatabase { get; set; }
        public object SourceState { get; set; }
        public object TargetState { get; set; }
        public Exception ThrownException { get; set; }
    }

    public class IoErrorEventArgs : EventArgs {

        public IoErrorEventArgs(Exception ex) {

            ThrownException = ex;
        }

        public Exception ThrownException { get; set; }
    }

    public class DataBaseErrorEventArgs : EventArgs {

        public DataBaseErrorEventArgs(string connectionString, string query, Exception ex) {

            ConnectionString = connectionString;
            QueryText = query;
            ThrownException = ex;
        }

        public string ConnectionString { get; set; }
        public string QueryText { get; set; }
        public Exception ThrownException { get; set; }
    }

    public class SuccessfulDatabaseConversionEventArgs : EventArgs {

        public SuccessfulDatabaseConversionEventArgs(string pathToDb, int convertedCount, int errorCount) {

            DatabasePath = pathToDb;
            InspectionsConverted = convertedCount;
            InspectionConversionErrors = errorCount;
        }

        public string DatabasePath { get; set; }
        public int InspectionsConverted { get; set; }
        public int InspectionConversionErrors { get; set; }
    }

    public class SuccessfulInspectionConversionEventArgs : EventArgs {

        public SuccessfulInspectionConversionEventArgs(string statusMessage) {

            ConversionStatusMessage = statusMessage;
        }

        public string ConversionStatusMessage { get; set; }
    }

    public class NumberOfInspectionsToConvertDiscoveredEventArgs : EventArgs {

        public NumberOfInspectionsToConvertDiscoveredEventArgs(int inspectionsToConvert) {

            NumberOfInspectionsToConvert = inspectionsToConvert;
        }

        public int NumberOfInspectionsToConvert { get; set; }

    }

    public class GeneralConversionNotificationEventArgs : EventArgs {

        public string Message { get; set; }

        public GeneralConversionNotificationEventArgs(string status) {
            Message = status;
        }
    }



}
