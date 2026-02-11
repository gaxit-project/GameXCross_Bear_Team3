using UnityEngine;
using UniRx;
using UniRx.Triggers;

[RequireComponent(typeof(Collider))]
public class CageTrap : MonoBehaviour
{
    private void Start()
    {
        this.OnTriggerEnterAsObservable()
            .Select(other => other.GetComponentInParent<TrapTarget>())
            .Where(target => target != null)
            .Subscribe(target =>
            {
                target.Capture(this.gameObject);
            })
            .AddTo(this);
    }

    //private void ActivateTrap(BearController bear)
    //{
    //    Debug.Log("クマが罠にかかりました。");

    //    bear.OnTrapped(this.gameObject);

    //    GetComponent<Collider>().enabled = false;
    //}
}
