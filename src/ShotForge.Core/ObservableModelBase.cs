using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShotForge.Core
{
    /// <summary>
    /// Gives a model real change notification so the UI can show live
    /// edits without needing to reload and replace the whole list (which
    /// would lose the current selection - exactly the case that matters
    /// most for editing: changing something you're currently looking at).
    ///
    /// Deliberately plain INotifyPropertyChanged, not
    /// CommunityToolkit.Mvvm's ObservableObject - Core has no UI
    /// framework dependency and shouldn't need one just for this.
    /// </summary>
    public abstract class ObservableModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
