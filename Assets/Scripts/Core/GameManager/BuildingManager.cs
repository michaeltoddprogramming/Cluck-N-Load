using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    public static UnityEvent onBuildingAdded = new UnityEvent();
    public static UnityEvent onBuildingRemoved = new UnityEvent();

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
        onBuildingAdded.Invoke();
    }

    public void removeBuilding(GameObject building)
    {
        buildings.Remove(building);
        onBuildingRemoved.Invoke();
    }

    public List<GameObject> getBrokenBuildings(char type)
    {
        List<GameObject> brokenBuildings = new List<GameObject>();

        StructureType structureType = StructureType.Animal;
        StructureType structureType2 = StructureType.Nothing;

        bool anyType = false;
     
        switch (type)
        {
            case 'C':
                structureType = StructureType.Animal;
                structureType2 = StructureType.Nothing;
                anyType = false;
                break;
            case 'A':
                structureType = StructureType.Barracks;
                structureType2 = StructureType.Nothing;
                break;
            case 'P':
                structureType = StructureType.CropPlot;
                structureType2 = StructureType.Silo;
                break;
            case 'S':
                structureType = StructureType.Defense;
                structureType2 = StructureType.Nothing;
                anyType = false;
                break;
            case 'X':
                anyType = true;
                break;
            default:
                Debug.LogWarning("Invalid type for repairAllBuildings");
                break;
        }
        
        foreach (GameObject building in buildings)
        {
            Structure bh = building.GetComponent<Structure>();
            if (bh != null && bh.canBeRepaired())
            {
                if(anyType)
                {
                    brokenBuildings.Add(building);
                }
                else if(structureType2 != StructureType.Nothing)
                {
                    if(bh.GetStructureType() == structureType || bh.GetStructureType() == structureType2)
                    {
                        brokenBuildings.Add(building);
                    }
                }
                else 
                {
                    if(bh.GetStructureType() == structureType)
                    {
                        brokenBuildings.Add(building);
                    }
                }
            }
        }
        return brokenBuildings;
    }

    public bool repairAllBuildings(char type)
    {        
        List<GameObject> brokenBuildings = getBrokenBuildings(type);

        foreach (GameObject building in brokenBuildings)
        {
            Structure bh = building.GetComponent<Structure>();
            if (bh != null)
            {
                bh.Repair();
            }
        }
        return true;
    }


}
