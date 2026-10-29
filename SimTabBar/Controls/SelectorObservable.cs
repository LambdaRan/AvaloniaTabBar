using System.ComponentModel;

namespace SimTabBar.Controls;

/// <summary>
/// 把「数据项 + 强类型取值委托」变成可推值的 <see cref="IObservable{T}"/>，交给
/// <see cref="AvaloniaObjectExtensions.Bind{T}(AvaloniaObject, AvaloniaProperty{T}, IObservable{T}, BindingPriority)"/>
/// 以 <see cref="BindingPriority.Template"/> 优先级写进 Header / IconSource。
///
/// 委托由使用方用 lambda 写（<c>item =&gt; ((Doc)item).Title</c>），编译期就静态引用了
/// 数据成员，因此 NativeAOT / 裁剪下不需要任何反射元数据 —— 这是本库 AOT 友好的
/// 代码侧取值方式（XAML 侧用 HeaderBinding / IconSourceBinding）。
/// 只订阅数据项自身的 INotifyPropertyChanged：委托里读到的嵌套对象变更无法自动感知，
/// 需要数据项自己转发通知。
/// </summary>
internal sealed class SelectorObservable<T> : IObservable<T>
{
    private readonly object _item;
    private readonly Func<object, T> _selector;

    internal SelectorObservable(object item, Func<object, T> selector)
    {
        _item = item;
        _selector = selector;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        var subscription = new Subscription(this, observer);
        subscription.Restart();
        return subscription;
    }

    private sealed class Subscription : IDisposable
    {
        private readonly SelectorObservable<T> _owner;
        private readonly IObserver<T> _observer;
        private readonly PropertyChangedEventHandler _onPropertyChanged;
        private bool _listening;
        private bool _disposed;

        internal Subscription(SelectorObservable<T> owner, IObserver<T> observer)
        {
            _owner = owner;
            _observer = observer;
            _onPropertyChanged = (_, _) => Emit();
        }

        internal void Restart()
        {
            if (_disposed) return;

            if (!_listening && _owner._item is INotifyPropertyChanged inpc) {
                inpc.PropertyChanged += _onPropertyChanged;
                _listening = true;
            }

            Emit();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_listening && _owner._item is INotifyPropertyChanged inpc)
                inpc.PropertyChanged -= _onPropertyChanged;
            _listening = false;
        }

        private void Emit()
        {
            if (_disposed) return;
            _observer.OnNext(_owner._selector(_owner._item));
        }
    }
}
