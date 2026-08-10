using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private GameObject[] weapons;
    private int _currentWeaponIndex = 0;

    private void Start()
    {
        SelectWeapon(_currentWeaponIndex);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectWeapon(1);
        }
    }

    private void SelectWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length) return;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].SetActive(i == index);
            }
        }
        
        _currentWeaponIndex = index;
        Debug.Log("Switched to weapon index: " + _currentWeaponIndex);
    }
}