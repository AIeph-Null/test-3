using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class GameMenuManager : MonoBehaviour
{
    public Transform head;
    public GameObject sliderObject1;
    public GameObject sliderObject2;
    public UnityEngine.UI.Slider slider1;
    public UnityEngine.UI.Slider slider2;
    public UnityEngine.UI.Toggle toggle;
    public float spawnDistance = 2;
    public GameObject menu;
    public InputActionProperty showButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    if (showButton.action.WasPressedThisFrame())
    {
      menu.SetActive(!menu.activeSelf);
      menu.transform.position = head.position + new Vector3(head.forward.x, 0, head.forward.z).normalized * spawnDistance;
    }

    menu.transform.LookAt(new Vector3(head.position.x, menu.transform.position.y, head.position.z));

    menu.transform.forward *= -1;

    

    if (toggle.isOn)
    {
    sliderObject2.SetActive(true);
    sliderObject1.SetActive(false);
    slider1.value = 0;
    //slider2.value = 0;
    }
    if (!toggle.isOn)
    {
    sliderObject2.SetActive(false);
    sliderObject1.SetActive(true);
    //slider1.value = -20;
    slider2.value = 0;
    }
    }
    
}
