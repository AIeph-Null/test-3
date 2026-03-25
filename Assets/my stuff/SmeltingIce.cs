using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SmeltingIce : MonoBehaviour
{
    
    public Slider age;
    private float ageNumber;
    public float x;
    public float y;
    public MeshRenderer glacier;

    public MeshCollider glacier2;
    public InputActionProperty showButton;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    ageNumber = age.value;


    if (ageNumber > x)
    {
      glacier.enabled  = false;
      glacier2.enabled = false;
    } 
    if (ageNumber < x)
    {
        glacier.enabled = true;
        glacier2.enabled = true;
    }

    //-200 to 35
    this.transform.position = new Vector3(150.0f, -ageNumber/40.0f + 5.0f, 100.0f);
    

    // −0.0000781191 * x^3 +0.0357838*x^2−4.94001 *x+305.744
    }
}

