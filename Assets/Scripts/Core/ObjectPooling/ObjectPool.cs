using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic object pool for improving performance by reusing objects
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    [Header("Pool Configuration")]
    [SerializeField] private List<Pool> pools = new List<Pool>();
    [SerializeField] private Transform poolParent;

    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    private void InitializePools()
    {
        if (poolParent == null)
        {
            poolParent = new GameObject("Object Pools").transform;
            poolParent.SetParent(transform);
        }

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // Create parent for this pool type
            Transform poolTypeParent = new GameObject($"{pool.tag} Pool").transform;
            poolTypeParent.SetParent(poolParent);

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, poolTypeParent);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }

        }

    /// <summary>
    /// Spawns an object from the pool
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        if (objectToSpawn == null)
        {
            Debug.LogWarning($"Pool {tag} is empty, creating new object");
            Pool pool = pools.Find(p => p.tag == tag);
            if (pool != null)
            {
                objectToSpawn = Instantiate(pool.prefab);
            }
            else
            {
                return null;
            }
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Get pooled object component if it exists
        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        pooledObj?.OnObjectSpawn();

        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }

    /// <summary>
    /// Returns an object to the pool
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist");
            Destroy(obj);
            return;
        }

        // Get pooled object component if it exists
        IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
        pooledObj?.OnObjectReturn();

        obj.SetActive(false);
        
        // Reset position to pool parent
        Transform poolTypeParent = poolParent.Find($"{tag} Pool");
        if (poolTypeParent != null)
        {
            obj.transform.SetParent(poolTypeParent);
        }
    }

    /// <summary>
    /// Returns an object to pool automatically after a delay
    /// </summary>
    public void ReturnToPoolAfterTime(string tag, GameObject obj, float delay)
    {
        StartCoroutine(ReturnAfterDelay(tag, obj, delay));
    }

    private System.Collections.IEnumerator ReturnAfterDelay(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null && obj.activeInHierarchy)
        {
            ReturnToPool(tag, obj);
        }
    }

    /// <summary>
    /// Creates a new pool at runtime
    /// </summary>
    public void CreatePool(string tag, GameObject prefab, int size)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} already exists");
            return;
        }

        Pool newPool = new Pool { tag = tag, prefab = prefab, size = size };
        pools.Add(newPool);

        Queue<GameObject> objectPool = new Queue<GameObject>();

        Transform poolTypeParent = new GameObject($"{tag} Pool").transform;
        poolTypeParent.SetParent(poolParent);

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, poolTypeParent);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }

        poolDictionary.Add(tag, objectPool);
        }

    /// <summary>
    /// Gets the current size of a pool
    /// </summary>
    public int GetPoolSize(string tag)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            return poolDictionary[tag].Count;
        }
        return 0;
    }
}

/// <summary>
/// Interface for objects that can be pooled
/// </summary>
public interface IPooledObject
{
    void OnObjectSpawn();
    void OnObjectReturn();
}
