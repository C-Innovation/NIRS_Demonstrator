using ReactiveUI;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NIRS_Demonstrator.Core;

public class ViewModelBase : ReactiveObject, INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// The event that is fired when any child property changes its value
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

    

    /// <summary>
    /// Call this to fire a <see cref="PropertyChanged"/> event
    /// </summary>
    /// <param name="name"></param>
    public void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged(this, new PropertyChangedEventArgs(name));
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
