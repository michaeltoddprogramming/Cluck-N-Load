using UnityEngine;
using System.Collections.Generic;

public class SpawnUnits : MonoBehaviour
{
    // public EnemyData enemyData1;
    // public EnemyData enemyData2;
    public EnemyData wolf;
    public EnemyData racoon;
    public EnemyData bear;
    public EnemyData boar;
    public GridDataGenerator _gridDataGenerator;

    private int maxSpawnAmount;
    private int minSpawnAmount;
    private int nightlySpawnMultiplier;
    private float seasonSpawnMultiplier;

    private void Awake()
    {
        _gridDataGenerator = FindObjectOfType<GridDataGenerator>();

        maxSpawnAmount = wolf.maxSpawnAmount;
        minSpawnAmount = wolf.minSpawnAmount;
        nightlySpawnMultiplier = wolf.nightlySpawnMultiplier;
        seasonSpawnMultiplier = wolf.seasonSpawnMultiplier;
    }

    public void SpawnEnemies(int season)
    {
        Debug.Log("this si the current season :" + season + "uaeydefgwoeuirygyihuofreiygufreigferwighuyfrewghifrewahgifdshgfdshgkfsdhjgfdshjgfds");
        if (wolf == null || _gridDataGenerator == null || racoon == null || bear == null || boar == null)
        {
            Debug.LogWarning("Missing EnemyData or GridDataGenerator!");
            return;
        }

        // Debug.Log($"Spawning enemies for season {season}************************************************************************************************************************************");

        switch (season)
        {
            case 1:
                {
                    int spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(wolf.Prefab, spawnPosition, Quaternion.identity);

                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }

                    // spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    // for (int i = 0; i < spawnCount; i++)
                    // {
                    //     Vector3 spawnPosition = GetRandomOutsidePosition();
                    //     GameObject enemyInstance = Instantiate(boar.Prefab, spawnPosition, Quaternion.identity);

                    //     EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                    //     if (enemyUnit != null)
                    //     {
                    //         CombatManager.Instance.RegisterUnit(enemyUnit);
                    //     }
                    // }


                    // spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    // for (int i = 0; i < spawnCount; i++)
                    // {
                    //     Vector3 spawnPosition = GetRandomOutsidePosition();
                    //     GameObject enemyInstance = Instantiate(racoon.Prefab, spawnPosition, Quaternion.identity);
                    //     Debug.Log("We spawned a racoon: " + enemyInstance + "-------------------------------------------------------");
                    //     EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                    //     if (enemyUnit != null)
                    //     {
                    //         CombatManager.Instance.RegisterUnit(enemyUnit);
                    //     }
                    // }


                    // spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    // for (int i = 0; i < spawnCount; i++)
                    // {
                    //     Vector3 spawnPosition = GetRandomOutsidePosition();
                    //     GameObject enemyInstance = Instantiate(bear.Prefab, spawnPosition, Quaternion.identity);

                    //     EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                    //     if (enemyUnit != null)
                    //     {
                    //         CombatManager.Instance.RegisterUnit(enemyUnit);
                    //     }
                    // }

                }
                break;
            case 2:
                {
                    int spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(wolf.Prefab, spawnPosition, Quaternion.identity);

                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }


                    spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(racoon.Prefab, spawnPosition, Quaternion.identity);
                        Debug.Log("We spawned a racoon: " + enemyInstance + "-------------------------------------------------------");
                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }
                }
                break;
            case 3:
                {
                    int spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(wolf.Prefab, spawnPosition, Quaternion.identity);

                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }


                    spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(racoon.Prefab, spawnPosition, Quaternion.identity);

                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }

                    spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(boar.Prefab, spawnPosition, Quaternion.identity);

                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }
                }
                break;
            case 4:
                {
                    int spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(wolf.Prefab, spawnPosition, Quaternion.identity);

                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }


                    spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(racoon.Prefab, spawnPosition, Quaternion.identity);

                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }

                    spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(boar.Prefab, spawnPosition, Quaternion.identity);

                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }

                    spawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

                    for (int i = 0; i < spawnCount; i++)
                    {
                        Vector3 spawnPosition = GetRandomOutsidePosition();
                        GameObject enemyInstance = Instantiate(bear.Prefab, spawnPosition, Quaternion.identity);

                        EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
                        if (enemyUnit != null)
                        {
                            CombatManager.Instance.RegisterUnit(enemyUnit);
                        }
                    }
                }
                break;
        }

    }

    // private Vector3 GetRandomOutsidePosition()
    // {
    //     if (_gridDataGenerator == null) return Vector3.zero;

    //     float spawnInset = 1f;

    //     int width = _gridDataGenerator.GetGridWidth();
    //     int height = _gridDataGenerator.GetGridHeight();


    //     int side = Random.Range(0, 4); // 0 = top, 1 = right, 2 = bottom, 3 = left
    //     float x = 0;
    //     float z = 0;

    //     switch (side)
    //     {
    //         case 0: // Top
    //             x = Random.Range(0, width);
    //             z = height - spawnInset;
    //             break;
    //         case 1: // Right
    //             x = width - spawnInset;
    //             z = Random.Range(0, height);
    //             break;
    //         case 2: // Bottom
    //             x = Random.Range(0, width);
    //             z = 0;
    //             break;
    //         case 3: // Left
    //             x = 0;
    //             z = Random.Range(0, height);
    //             break;
    //     }

    //     float y = 0f; // or terrain.SampleHeight() if you have elevation
    //     return new Vector3(x, y, z);
    // }

    //     private Vector3 GetRandomOutsidePosition()
    // {
    //     if (_gridDataGenerator == null) return Vector3.zero;

    //     float spawnInset = 1f;

    //     int width = _gridDataGenerator.GetGridWidth();
    //     int height = _gridDataGenerator.GetGridHeight();

    //     while (true)
    //     {
    //         int side = Random.Range(0, 4); // 0 = top, 1 = right, 2 = bottom, 3 = left
    //         int x = 0;
    //         int y = 0;

    //         switch (side)
    //         {
    //             case 0: // Top
    //                 x = Random.Range(0, width);
    //                 y = height - 1;
    //                 break;
    //             case 1: // Right
    //                 x = width - 1;
    //                 y = Random.Range(0, height);
    //                 break;
    //             case 2: // Bottom
    //                 x = Random.Range(0, width);
    //                 y = 0;
    //                 break;
    //             case 3: // Left
    //                 x = 0;
    //                 y = Random.Range(0, height);
    //                 break;
    //         }

    //         GridCell cell = _gridDataGenerator.GetCell(x, y);
    //         if (cell != null && !cell.flags.isObstacle && !cell.flags.isOccupied)
    //         {
    //             Vector3 position = cell.worldPosition;

    //             if (side == 0) position.z += spawnInset;
    //             else if (side == 1) position.x += spawnInset;
    //             else if (side == 2) position.z -= spawnInset;
    //             else if (side == 3) position.x -= spawnInset;

    //             position.y = 0f; // or terrain height

    //             return position;
    //         }
    //         // else loop again to pick a new position
    //     }
    // }


    private Vector3 GetRandomOutsidePosition()
    {
        if (_gridDataGenerator == null) return Vector3.zero;

        int width = _gridDataGenerator.GetGridWidth();
        int height = _gridDataGenerator.GetGridHeight();
        // Debug.Log($"Grid Size: {width}x{height}+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++------------------------------------------------------");

        while (true) // Keep trying until valid
        {
            int side = Random.Range(0, 4);
            int x = 0;
            int y = 0;

            switch (side)
            {
                case 0: // Top edge
                    x = Random.Range(0, width);
                    y = height - 1;
                    break;
                case 1: // Right edge
                    x = width - 1;
                    y = Random.Range(0, height);
                    break;
                case 2: // Bottom edge
                    x = Random.Range(0, width);
                    y = 0;
                    break;
                case 3: // Left edge
                    x = 0;
                    y = Random.Range(0, height);
                    break;
            }

            GridCell cell = _gridDataGenerator.GetCell(x, y);

            if (cell != null && !cell.flags.isObstacle && !cell.flags.isOccupied)
            {
                // Spawn exactly at the grid cell position
                return cell.worldPosition;
            }
            // else retry with another random edge cell
        }
    }

    public void increaseAfterNight()
    {
        maxSpawnAmount += nightlySpawnMultiplier;
        minSpawnAmount += nightlySpawnMultiplier;
        // Debug.Log($"max: {maxSpawnAmount} min: {minSpawnAmount}************************************************");

    }
    public void increaseAfterSeason()
    {
        // maxSpawnAmount = Mathf.CeilToInt(maxSpawnAmount * seasonSpawnMultiplier);
        // minSpawnAmount = Mathf.CeilToInt(minSpawnAmount * seasonSpawnMultiplier);
        // Debug.Log($"max: {maxSpawnAmount} min: {minSpawnAmount}===============================================");

        nightlySpawnMultiplier = Mathf.CeilToInt(nightlySpawnMultiplier * seasonSpawnMultiplier);
        // Debug.Log($"nightly things: {nightlySpawnMultiplier}=====================================================");
    }



}
