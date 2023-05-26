using PicoApp.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoApp.ViewModel
{
    internal class PicoViewModel : ViewModelBase
    {
        public PicoViewModel()
        {
            PicoData = new ObservableCollection<PicoData>();
            
        }
        private ObservableCollection<PicoData>? picoData;
        public ObservableCollection<PicoData> PicoData
        {
            get { return picoData; }
            set { picoData = value; OnPropertyChanged(); }
        }
    }
}
