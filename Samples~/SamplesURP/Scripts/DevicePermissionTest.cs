using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevicePermissionTest : MonoBehaviour
{
    public TextMeshProUGUI DebugText;
    public TextMeshProUGUI RunningText;
    [Space]
    // Variables to store the highest acceleration values for each axis
    private float maxAccelX = float.MinValue;
    private float maxAccelY = float.MinValue;
    private float maxAccelZ = float.MinValue;
    private float maxGForce = float.MinValue;
    
    public bool RunningDataCapture = false;
    public Vector3 recordData;
    public void StartCapture(){
        RunningDataCapture = true;
    }
        
    public void StopCapture(){
        RunningDataCapture = false;
       
        //reset the max values
        StartCoroutine(DelayClearValues());

    }
    IEnumerator DelayClearValues()
    {
        yield return new WaitForEndOfFrame();
        if(recordData.x<maxAccelX){
            recordData.x = maxAccelX;
            recordData.x = Mathf.Round(recordData.x * 100f) / 100f;
        }
        if(recordData.y<maxAccelY){
            recordData.y = maxAccelY;
            recordData.y = Mathf.Round(recordData.y * 100f) / 100f;
        }
        if(recordData.z<maxAccelZ){
            recordData.z = maxAccelZ;
            recordData.z = Mathf.Round(recordData.z * 100f) / 100f;
        }
        
        
       
        DebugText.text+= $"\nRecord Accel X: {recordData.x},{recordData.y},{recordData.z}";
        DebugText.text+=$"\nMax Recent GForce: {maxGForce}";
        maxAccelX = float.MinValue;
        maxAccelY = float.MinValue;
        maxAccelZ = float.MinValue;
        maxGForce = float.MinValue;
    }

    public void Update()
    {
        if(!RunningDataCapture){
            return;
        }
        // Get the acceleration data from the device's IMU
        Vector3 acceleration = Input.acceleration;

        //cap the value to 2 decimals
        acceleration.x = Mathf.Round(acceleration.x * 100f) / 100f;
        acceleration.y = Mathf.Round(acceleration.y * 100f) / 100f;
        acceleration.z = Mathf.Round(acceleration.z * 100f) / 100f;
        // Log the IMU data to the console (for debugging)
        Debug.Log($"Acceleration: X = {acceleration.x}, Y = {acceleration.y}, Z = {acceleration.z}");

        // Example: Using the acceleration data to move an object
        // transform.Translate(acceleration * Time.deltaTime);
        // Compare and update the highest acceleration on X axis
        if (acceleration.x > maxAccelX)
        {
            maxAccelX = acceleration.x;
            DebugText.text = $"Max Accel X: {maxAccelX}\nMax Accel Y: {maxAccelY}\nMax Accel Z: {maxAccelZ}";
        }

        // Compare and update the highest acceleration on Y axis
        if (acceleration.y > maxAccelY)
        {
            maxAccelY = acceleration.y;
            DebugText.text = $"Max Accel X: {maxAccelX}\nMax Accel Y: {maxAccelY}\nMax Accel Z: {maxAccelZ}";
        }

        // Compare and update the highest acceleration on Z axis
        if (acceleration.z > maxAccelZ)
        {
            maxAccelZ = acceleration.z;
            DebugText.text = $"Max Accel X: {maxAccelX}\nMax Accel Y: {maxAccelY}\nMax Accel Z: {maxAccelZ}";
        }

        // Calculate the total g-force magnitude
        float gForce = Mathf.Sqrt(acceleration.x * acceleration.x + 
                                  acceleration.y * acceleration.y + 
                                  acceleration.z * acceleration.z);

        // Compare and store the highest g-force recorded
        if (gForce > maxGForce)
        {
            maxGForce = gForce;
        }
        RunningText.text = $"GForce: {gForce}\n({acceleration.x},{acceleration.y},{acceleration.z})";
    }
}
