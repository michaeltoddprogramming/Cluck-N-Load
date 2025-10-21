using UnityEngine;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    public List<GameObject> buildings = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // Assign the singleton
        }
        else
        {
            Destroy(gameObject); // Ensure only one exists
        }
    }

    // void Start()
    // {
        
    // }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }

    public void addBuilding(GameObject building)
    {
        buildings.Add(building);
    }

    public void removeBuilding(GameObject building)
    {
        buildings.Remove(building);
    }

    public List<GameObject> getBrokenBuildings()
    {
        List<GameObject> brokenBuildings = new List<GameObject>();
        
        foreach (GameObject building in buildings)
        {
            Structure bh = building.GetComponent<Structure>();
            if (bh != null && bh.canBeRepaired())
            {
                brokenBuildings.Add(building);
            }
        }
        return brokenBuildings;
    }
}
