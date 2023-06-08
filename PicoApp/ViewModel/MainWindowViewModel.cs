using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicoApp.ViewModel
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private OscopeViewModel oscopeViewModel;
        private DHMViewModel dHMViewModel;

        public MainWindowViewModel()
        {
            OscopeViewModel = new OscopeViewModel();
            DHMViewModel = new DHMViewModel();
        }
        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (OscopeViewModel.NiSession == null) return;
            OscopeViewModel.NiSession.Dispose();
        }
        public OscopeViewModel OscopeViewModel
        {
            get { return oscopeViewModel; }
            set { oscopeViewModel = value; OnPropertyChanged(); }
        }
        public DHMViewModel DHMViewModel
        {
            get { return dHMViewModel; }
            set { dHMViewModel = value; OnPropertyChanged(); }
        }
    }
}
