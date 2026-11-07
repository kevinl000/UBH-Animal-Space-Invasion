using UnityEngine;

public class PersistentSystemsController : MonoBehaviour
{
    private static PersistentSystemsController instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
            return;

        var prefab = Resources.Load<PersistentSystemsController>("PersistentManager");
        if (prefab == null)
        {
            Debug.LogError("The PersistentManager prefab is missing from Resources.");
            return;
        }

        Instantiate(prefab);
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
