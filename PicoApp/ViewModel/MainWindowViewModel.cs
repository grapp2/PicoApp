using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoApp.ViewModel
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private PicoViewModel picoViewModel;
        private DHMViewModel dHMViewModel;

        public MainWindowViewModel()
        {
            PicoViewModel = new PicoViewModel();
            DHMViewModel = new DHMViewModel();
        }

        public PicoViewModel PicoViewModel
        {
            get { return picoViewModel; }
            set { picoViewModel = value; OnPropertyChanged(); }
        }
        public DHMViewModel DHMViewModel
        {
            get { return dHMViewModel; }
            set { dHMViewModel = value; OnPropertyChanged(); }
        }
    }
}
