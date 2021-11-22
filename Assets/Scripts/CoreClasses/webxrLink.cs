using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebXR;

public class webxrLink : MonoBehaviour
{
    public WebXRController controller;

    void Update()
    {
        if (controller != null)
        {
            transform.position = controller.transform.position;
            transform.rotation = controller.transform.rotation;
        }
    }
}
