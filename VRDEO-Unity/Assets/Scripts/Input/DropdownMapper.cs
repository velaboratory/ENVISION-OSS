using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownMapper : MonoBehaviour
{
    public LoginInput input;
    public Dropdown dropdown;

    public void Start()
    {
        dropdown = dropdown ? dropdown : GetComponent<Dropdown>();
        input = input ? input : FindObjectOfType<LoginInput>();
    }

    public void valToString(int selection)
    {
        input.contentId = dropdown.options[selection].text;
    }
}
