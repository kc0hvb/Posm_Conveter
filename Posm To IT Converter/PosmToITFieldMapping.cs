using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace PicAx_To_IT_Converter {
    
    [Serializable]
    public class PosmToITFieldMapping : INotifyPropertyChanged {

        public PosmToITFieldMapping() {

        }

        private ObservableCollection<MappingValue> internalMappingValues = new ObservableCollection<MappingValue>();

        public ObservableCollection<MappingValue> MappingValues {
            get {
                return internalMappingValues;
            }
            set {
                internalMappingValues = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MappingValues)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    [Serializable]
    public class MappingValue : INotifyPropertyChanged {

        public MappingValue() {

        }


        private ObservableCollection<string> internalPosmFields;

        public ObservableCollection<string> posmFields {
            get {
                return internalPosmFields;
            }
            set {
                internalPosmFields = value;
                NotifyPropertyChanged();
            }
        }


        private string internalPosmTableName;

        public string posmTableName {
            get {
                return internalPosmTableName;
            }
            set {
                internalPosmTableName = value;
                NotifyPropertyChanged();
            }
        }


        private string internalItpipesFieldName;

        public string itpipesFieldName {
            get {
                return internalItpipesFieldName;
            }
            set {
                internalItpipesFieldName = value;
                NotifyPropertyChanged();
            }
        }


        private string internalItpipesTableName;

        public string itpipesTableName {
            get {
                return internalItpipesTableName;
            }
            set {
                internalItpipesTableName = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
