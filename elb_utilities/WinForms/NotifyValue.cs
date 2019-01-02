using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace elb_utilities.WinForms
{
    public class NotifyValue<T> : INotifyPropertyChanged
    {
        private T _value;

        public event PropertyChangedEventHandler PropertyChanged;

        public T Value
        {
            get { return _value; }
            set { _value = value; NotifyPropertyChanged(); }
        }

        public NotifyValue(T value)
        {
            _value = value;
        }

        public static implicit operator T(NotifyValue<T> notifyValue)
        {
            return notifyValue.Value;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
