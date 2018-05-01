using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarGenerator : MonoBehaviour {

    public GameObject carPrefab;
    public GameObject newCar;
    public CarEngine engine;
    private bool CarStatus = false;

    // Use this for initialization
    void Start() {
        newCar = Instantiate(carPrefab, new Vector3(102.08f, 0.071f, 67.99f), transform.rotation);
        CarEngine engine = carPrefab.GetComponent<CarEngine>();
        engine.path = GameObject.Find("Path").transform;
    }
	
	// Update is called once per frame
	void Update () 
    {
        // if car has reached end, then delete it
        if (newCar.transform.position.z > 250)
        {
            CarStatus = true;
        }

        // if car has to be deleted, then delete and recreate
        if (CarStatus)
        {
            Destroy(newCar);
            CarStatus = false;
            Start();
        }
    }
}
