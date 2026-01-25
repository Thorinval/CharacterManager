namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service singleton pour notifier les composants des changements de mode adulte
/// </summary>
public interface IAdultModeNotificationService
{
    /// <summary>
    /// Définit le mode adulte et notifie les observateurs
    /// </summary>
    void SetAdultMode(bool isEnabled);

    /// <summary>
    /// Obtient l'état actuel du mode adulte
    /// </summary>
    bool IsAdultModeEnabled { get; }

    /// <summary>
    /// S'abonner aux changements de mode adulte
    /// </summary>
    void Subscribe(Action<bool> callback);

    /// <summary>
    /// Se désabonner des changements de mode adulte
    /// </summary>
    void Unsubscribe(Action<bool> callback);
}
