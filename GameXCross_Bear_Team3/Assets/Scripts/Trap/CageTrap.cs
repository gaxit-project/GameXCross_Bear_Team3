using UnityEngine;
using UniRx;
using UniRx.Triggers;

[RequireComponent(typeof(Collider))]
public class CageTrap : MonoBehaviour
{
    private void Start()
    {
        this.OnTriggerEnterAsObservable()
            .First()
            .Select(other => other.GetComponent<TrapTarget>())
            .Where(target => target != null)
            .Subscribe(target =>
            {
                target.Capture(this.gameObject);
            });
    }

    //private void ActivateTrap(BearController bear)
    //{
    //    Debug.Log("クマが罠にかかりました。");

    //    bear.OnTrapped(this.gameObject);

    //    GetComponent<Collider>().enabled = false;
    //}
}
