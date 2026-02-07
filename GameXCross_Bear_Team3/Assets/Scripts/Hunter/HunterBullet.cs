using UnityEngine;

public class HunterBullet : MonoBehaviour
{
    private float _speed;
    private Transform _target;
    private float _damage;
    private HunterController _hunter;

    public void Launch(Transform target, float speed, float damage, HunterController hunter)
    {
        _target = target;
        _speed = speed;
        _damage = damage;
        _hunter = hunter;

        // 当たらなければ一定時間後に削除
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        if(_target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = (_target.position + Vector3.up * 8.0f - transform.position).normalized;

        transform.position += direction * _speed * Time.deltaTime;
        transform.forward = direction;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Bear"))
        {
            var bear = other.GetComponent<BearController>();
            if (bear != null)
            {
                bear.TakeDamage(_damage, _hunter);
            }

            Destroy(gameObject);
        }
        else if (!other.CompareTag("Hunter") && (other.CompareTag("Ground") || other.CompareTag("House")))
        {
            Destroy(gameObject);
        }
    }
}
