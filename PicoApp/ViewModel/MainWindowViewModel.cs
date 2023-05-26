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
        public MainWindowViewModel()
        {
            PicoViewModel = new PicoViewModel();
        }

        public PicoViewModel PicoViewModel
        {
            get { return picoViewModel; }
            set { picoViewModel = value; OnPropertyChanged(); }
        }
    }
}
