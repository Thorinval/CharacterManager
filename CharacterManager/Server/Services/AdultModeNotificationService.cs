namespace CharacterManager.Server.Services;

/// <summary>
/// Service singleton pour notifier les composants des changements de mode adulte
/// </summary>
public class AdultModeNotificationService
{
    private bool _isAdultModeEnabled = true;
    private readonly List<Action<bool>> _callbacks = new();
    private readonly object _lock = new();

    /// <summary>
    /// Définit le mode adulte et notifie les observateurs
    /// </summary>
    public void SetAdultMode(bool isEnabled)
    {
        if (_isAdultModeEnabled != isEnabled)
        {
            _isAdultModeEnabled = isEnabled;
            NotifyObservers(isEnabled);
        }
    }

    /// <summary>
    /// Obtient l'état actuel du mode adulte
    /// </summary>
    public bool IsAdultModeEnabled => _isAdultModeEnabled;

    /// <summary>
    /// S'abonner aux changements de mode adulte
    /// </summary>
    public void Subscribe(Action<bool> callback)
    {
        lock (_lock)
        {
            if (!_callbacks.Contains(callback))
            {
                _callbacks.Add(callback);
            }
        }
    }

    /// <summary>
    /// Se désabonner des changements de mode adulte
    /// </summary>
    public void Unsubscribe(Action<bool> callback)
    {
        lock (_lock)
        {
            _callbacks.Remove(callback);
        }
    }

    private void NotifyObservers(bool isEnabled)
    {
        List<Action<bool>> callbacksCopy;
        lock (_lock)
        {
            callbacksCopy = new List<Action<bool>>(_callbacks);
        }

        foreach (var callback in callbacksCopy)
        {
            try
            {
                callback(isEnabled);
            }
            catch (Exception ex)
            {
                // Log silencieusement les erreurs pour éviter de casser la notification
                System.Diagnostics.Debug.WriteLine($"Erreur dans callback AdultMode: {ex.Message}");
            }
        }
    }
}


