using System.Collections.Generic;
using UnityEngine;

public class Pool : MonoBehaviour
{
    public static Pool Instance;

    private Dictionary<GameObject, Queue<GameObject>> poolDict = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        Instance = this;
    }

    public GameObject Get(GameObject prefab)
    {
        if (!poolDict.ContainsKey(prefab))
        {
            poolDict[prefab] = new Queue<GameObject>();
        }

        if (poolDict[prefab].Count > 0)
        {
            GameObject obj = poolDict[prefab].Dequeue();
            obj.SetActive(true);
            return obj;
        }

        GameObject newObj = Instantiate(prefab);
        newObj.SetActive(true);
        return newObj;
    }

    public void Release(GameObject obj)
    {
        obj.SetActive(false);
        if (!poolDict.ContainsKey(obj)) return; // optional safety
        poolDict[obj].Enqueue(obj);
    }
}
