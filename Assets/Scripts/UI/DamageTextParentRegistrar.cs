using UnityEngine;

public class DamageTextParentRegistrar : MonoBehaviour
{
    private void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterDamageTextParent(transform);
    }
}
