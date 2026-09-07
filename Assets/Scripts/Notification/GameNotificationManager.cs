using UnityEngine;

public class GameNotificationManager : SingletonMonoBehaviour<GameNotificationManager>
{
    private INotificationService _notificationService;

    private void Awake()
    {
        _notificationService = CreateNotificationService();
    }

    private INotificationService CreateNotificationService()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            return new AndroidNotificationService();
        }
        return new NullNotificationService();
    }

    public void Initialize()
    {
        _notificationService?.Initialize();
    }

    public void OnApplicationQuit()
    {
        _notificationService?.OnApplicationQuit();
    }
}
