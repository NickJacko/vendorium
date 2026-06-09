using UnityEngine;

namespace Vendorium
{
    // Sitzt auf einem Child-GameObject mit Sphere Collider (IsTrigger = true, Radius = 2m).
    // Meldet Kunden-Eintritte/Austritte an die übergeordnete VendingMachine.
    public class MachineTriggerZone : MonoBehaviour
    {
        private VendingMachine _machine;

        private void Awake()
        {
            _machine = GetComponentInParent<VendingMachine>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Customer") && _machine != null)
                _machine.OnCustomerEnterRange();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Customer") && _machine != null)
                _machine.OnCustomerLeaveRange();
        }
    }
}
