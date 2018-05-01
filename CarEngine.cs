using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngine : MonoBehaviour {

    public Transform path;
    private List<Transform> nodes;
    private int currentNode = 0;

    // wheel colliders for vehicle
    public WheelCollider wheelFrontLeft;
    public WheelCollider wheelFrontRight;
    public WheelCollider wheelBackLeft;
    public WheelCollider wheelBackRight;

    // vehicle related
    public float maxSteerAngle = 45f; // car wheels max steering angle
    public float targetSteerAngle = 0f;
    public float turnSpeed = 5f;
    public float maxMotorTorque = 80f;
    public float maxBrakeTorque = 150f;
    public float currentSpeed;
    public float maxSpeed = 200f;
    public Vector3 centerOfMass;
    public Vector3 vehicleStartPos;
    public bool teleportStatus = false;

    // brake status
    public bool brakeStatus = false;
    //public bool avoidObstacles = false;
    public Texture2D textureNormal;
    public Texture2D textureBraking;
    public Renderer carRenderer;

    // vehicle sensors
    [Header("Sensors")]
    public float sensorLength = 15f;
    public Vector3 frontSensorPosition = new Vector3(0f, 0.5f, 1.7f);
    public float frontSideSensorPos = 0.8f; // position of front sensor
    public float frontAngledSensor = 40f; // angle of sensor vision

    // distance to stop sign & identifying stop sign
    [Header("Stopping")]
    public float distanceToStop = 15f;
    public GameObject stopSign = GameObject.Find("stop_sign");
    // vehicle stop sign detection
    public int stopStatus = 0;
    // pedestrian status
    public bool pedestrianStatus = false; // can be any object but for the study, it probably would be a pedestrian

    // Use this for initialization
    void Start () {
        stopSign = GameObject.Find("stop_sign");
        GetComponent<Rigidbody>().centerOfMass = centerOfMass;
        Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        for (int i = 0; i < pathTransforms.Length; i++)
        {
            if (pathTransforms[i] != path.transform)
            {
                nodes.Add(pathTransforms[i]);
            }
        }
    }
	
	// Update is called once per frame
	private void FixedUpdate ()
    {
        Sensors();
        CheckForStop();
        WaypointDistance();
        LerpToSteerAngle();
        ApplySteer();
        Drive();
        Brake();
    }

    // vehicle sensors
    private void Sensors()
    {
        RaycastHit hit;
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * frontSensorPosition.z;
        sensorStartPos += transform.up * frontSensorPosition.y;
        float avoidMultiplier = 0;
        //avoidObstacles = false;
        pedestrianStatus = false;
        brakeStatus = false;

        // Front right sensor
        sensorStartPos += transform.right * frontSideSensorPos;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
        {
            if (hit.collider.CompareTag("Terrain") == false)
            {   
                Debug.DrawLine(sensorStartPos, hit.point);
                // Pedestrian spotted
                if (hit.collider.CompareTag("Player") == true)
                {
                    pedestrianStatus = true;
                }
                // Other obstacle spotted
                //else
                //{
                //    avoidObstacles = true;
                //    avoidMultiplier -= 1f;
                //}

            }
        }

        //Front right angled sensor
        else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(frontAngledSensor, transform.up) * transform.forward, out hit, sensorLength))
        {
            if (hit.collider.CompareTag("Terrain") == false)
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                // Pedestrian spotted
                if (hit.collider.CompareTag("Player") == true)
                {
                    pedestrianStatus = true;
                }
                // Other obstacle spotted
                //else
                //{
                //    avoidObstacles = true;
                //    avoidMultiplier -= 0.5f;
                //}
            }
        }

        //Front left sensor
        sensorStartPos -= 2 * transform.right * frontSideSensorPos;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
        {
            if (hit.collider.CompareTag("Terrain") == false)
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                // Pedestrian spotted
                if (hit.collider.CompareTag("Player") == true)
                {
                    pedestrianStatus = true;
                }
                // Other obstacle spotted
                //else
                //{
                //    avoidObstacles = true;
                //    avoidMultiplier += 1f;
                //}
            }
        }

        //Front left angled sensor
        else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-frontAngledSensor, transform.up) * transform.forward, out hit, sensorLength))
        {
            if (hit.collider.CompareTag("Terrain") == false)
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                // Pedestrian spotted
                if (hit.collider.CompareTag("Player") == true)
                {
                    pedestrianStatus = true;
                }
                // Other obstacle spotted
                //else
                //{
                //    avoidObstacles = true;
                //    avoidMultiplier += 0.5f;
                //}
            }
        }

        //Front middle sensor
        sensorStartPos += transform.right * frontSideSensorPos;
        if (avoidMultiplier == 0)
        {
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") == false)
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    // Pedestrian spotted
                    if (hit.collider.CompareTag("Player") == true)
                    {
                        pedestrianStatus = true;
                    }
                    // Other obstacle spotted
                    //else
                    //{
                    //    avoidObstacles = true;
                    //    if (hit.normal.x < 0)
                    //    {
                    //        avoidMultiplier = -1;
                    //    }
                    //    else
                    //    {
                    //        avoidMultiplier = 1;
                    //    }
                    //}
                }
            }
        }

        // If pedestrian is spotted then apply brakes
        if (pedestrianStatus)
        {
            brakeStatus = true;
        }
        else brakeStatus = false;

        //if (avoidObstacles)
        //{
        //    targetSteerAngle = maxSteerAngle * avoidMultiplier;
        //}

    }

    // choosing steering angles
    private void ApplySteer()
    {
        //if (avoidObstacles) return;
        Vector3 relativeVector = transform.InverseTransformPoint(nodes[currentNode].position);
        float newSteer = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle; // Wheel angle
        targetSteerAngle = newSteer;
    }

    // applying motor torque
    private void Drive()
    {
        currentSpeed = 2 * Mathf.PI * wheelFrontLeft.radius * wheelFrontLeft.rpm * 60 / 1000;
        if (currentSpeed < maxSpeed && brakeStatus == false)
        {
            wheelFrontLeft.motorTorque = maxMotorTorque;
            wheelFrontRight.motorTorque = maxMotorTorque;
        }
        else
        {
            wheelFrontLeft.motorTorque = 0;
            wheelFrontRight.motorTorque = 0;
        }
    }

    // smoother turns
    private void LerpToSteerAngle()
    {
        wheelFrontLeft.steerAngle = Mathf.Lerp(wheelFrontLeft.steerAngle, targetSteerAngle, Time.deltaTime * turnSpeed);
        wheelFrontRight.steerAngle = Mathf.Lerp(wheelFrontRight.steerAngle, targetSteerAngle, Time.deltaTime * turnSpeed);
    }

    // braking vehicle
    private void Brake()
    {
        if (brakeStatus)
        {
            carRenderer.material.mainTexture = textureBraking;
            wheelBackLeft.brakeTorque = maxBrakeTorque;
            wheelBackRight.brakeTorque = maxBrakeTorque;
        }
        else
        {
            carRenderer.material.mainTexture = textureNormal;
            wheelBackLeft.brakeTorque = 0;
            wheelBackRight.brakeTorque = 0;
        }
        
    }

    // Check whether to stop the vehicle at stop sign
    private void CheckForStop()
    {   
        float stopTimer = 0f; // time at stop sign
        if ((stopSign.transform.position.z - transform.position.z) < 30f & stopStatus == 0)
        {
            stopStatus = 1;
        }

        // if vehicle has to stop then do these
        if (stopStatus == 1)
        {
            brakeStatus = true;
            Brake();
            while (currentSpeed < 0.05 & stopStatus == 1)
            {  
                stopTimer += Time.deltaTime;
                if (stopTimer > 500000)
                {
                    stopStatus = 2;
                    brakeStatus = false;
                }
            }
        }
    }

    // determine distance to waypoint
    private void WaypointDistance()
    {
        if (Vector3.Distance(transform.position, nodes[currentNode].position) < 5f)
        {
            // For nodes that are not the last node
            if (currentNode != (nodes.Count - 1))
            {
                currentNode++;
            }
            // If last node, then apply braking
            else
            {
                //brakeStatus = true;
                //teleportStatus = true;
                currentNode = 0;
                stopStatus = 0;
            }
        }
    }

}
