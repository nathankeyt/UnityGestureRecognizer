

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGameObjectCamera : MonoBehaviour
{
    public static Camera Instance;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
            
        Instance = GetComponent<UnityEngine.Camera>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}