using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginInput : MonoBehaviour
{
    public string userId;
    public string roomId;
    public string contentId;

    private void Awake()
    {
        userId = userId.Trim() == "" ? System.Guid.NewGuid().ToString() : userId;
    }
}
