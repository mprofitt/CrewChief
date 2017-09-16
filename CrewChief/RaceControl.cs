using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChief
{
    class RaceControl : INotifyPropertyChanged
    {
        private bool _driverMarker;
        public bool DriverMarker
        {
            get { return this._driverMarker; }
            set
            {
                if (this._driverMarker != value)
                {
                    _driverMarker = value;
                    OnPropertyChanged("DriverMarker");
                }
            }
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
