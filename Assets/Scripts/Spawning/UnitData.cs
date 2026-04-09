using UnityEngine;

public class UnitData
{
    public int id; // use this to know which GameObject to destroy in UnitManager
    public GameObject gameObject; // reference to actual, spawned unit root
    public UnitManager.UnitConfig config; // this data is used to spawn the unit and configure its stats
}
