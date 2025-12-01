using UnityEngine;
using UniRx;
using UniRx.Triggers;

[RequireComponent(typeof(Collider))]
public class CageTrap : MonoBehaviour
{
    private void Start()
    {
        this.OnTriggerEnterAsObservable()
            .Subscribe(collider =>
            {
                var bear = collider.GetComponent<BearController>();
                if (bear != null)
                {
                    ActivateTrap(bear);
                }
            })
            .AddTo(this);
    }

    private void ActivateTrap(BearController bear)
    {
        Debug.Log("ŸB‚ÉŒF‚ª“ü‚è‚Ü‚µ‚½B");

        bear.OnTrapped(transform.position);

        bear.transform.position = transform.position;

        GetComponent<Collider>().enabled = false;
    }
}