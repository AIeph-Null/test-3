using UnityEngine;
using UnityEngine.UI;

public class TransformWater : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Slider age;
    public Slider age2;
    private float ageNumber;
    private float ageNumber2;
    public int version;
    void Start()
    {

        
    }

    // Update is called once per frame
    void Update()
    {
        ageNumber = age.value;
        ageNumber2 = age2.value;
        if (version == 1) { //water 
            this.transform.position = new Vector3(150.0f, ageNumber, 100.0f);
        }
        else if (version == 2) { //big iceberg 
            if (ageNumber > 0) {ageNumber = 0;}
            this.transform.position = new Vector3(62.36708f, ageNumber, 141.2893f);
        }
        //when -2, get -14 (previous: when -2.5 get -12?)
        //when 0, do 2
        else if (version == 3) { //small iceberg
            ageNumber = ageNumber  + 12.0f; //when -2, get -2, when 12 get 0 
            if (ageNumber > -2) {ageNumber = ageNumber*0.142857f -1.71429f;} //0.142857x−1.71429
            if (age.value > 15) {ageNumber = -30;}
            this.transform.position = new Vector3(62.36708f, ageNumber, 141.2893f);
        }
    }   //-ageNumber2/40.0f + 5.0f
}
