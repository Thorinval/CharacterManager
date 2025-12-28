namespace CharacterManager.Server.Services;

using Microsoft.AspNetCore.Components;

public interface IModalService
{
    event Action<Type, Dictionary<string, object>?, ModalSize>? OnShow;
    event Action? OnClose;

    void Open<T>(Dictionary<string, object>? parameters = null, ModalSize size = ModalSize.Medium)
        where T : IComponent;

    void Close();
}

public class ModalService : IModalService
{
    public event Action<Type, Dictionary<string, object>?, ModalSize>? OnShow;
    public event Action? OnClose;

    public void Open<T>(Dictionary<string, object>? parameters = null, ModalSize size = ModalSize.Medium)
        where T : IComponent
    {
        OnShow?.Invoke(typeof(T), parameters, size);
    }

    public void Close()
    {
        OnClose?.Invoke();
    }
}
