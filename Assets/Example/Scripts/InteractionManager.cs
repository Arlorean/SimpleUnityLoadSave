using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public void Quit() {
        Application.Quit();
    }

    void Update()
    {
        var hitInfo = new RaycastHit();
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hitInfo)) {
            if (Input.GetMouseButtonDown(0)) {
                hitInfo.rigidbody?.AddForceAtPosition(100*Random.onUnitSphere, hitInfo.point);
            }
        }
    }
}
